from __future__ import annotations

from enum import Enum
from typing import Any

from pydantic import BaseModel, Field, model_validator


class MinigameParameterType(str, Enum):
    ENUM = "enum"
    INT = "int"
    FLOAT = "float"
    BOOL = "bool"
    STRING = "string"


class MinigameStringMode(str, Enum):
    TEXT = "text"
    ID_REFERENCE = "id_reference"


class MinigameGenerationContext(BaseModel):
    fit_tags: list[str] = Field(default_factory=list)
    world_state: list[str] = Field(default_factory=list)
    difficulty_band: str | None = None


class MinigameCouplingRule(BaseModel):
    rule_id: str = Field(min_length=1)
    description: str = Field(min_length=1)


class MinigameGeneratorValidationRequest(BaseModel):
    parameters: dict[str, Any] = Field(default_factory=dict)
    context: MinigameGenerationContext = Field(default_factory=MinigameGenerationContext)


class MinigameGeneratorValidationResult(BaseModel):
    is_valid: bool
    generator_id: str
    errors: list[str] = Field(default_factory=list)
    resolved_parameters: dict[str, Any] = Field(default_factory=dict)
    fallback_generator_ids: list[str] = Field(default_factory=list)


class MinigameParameterDefinition(BaseModel):
    name: str = Field(min_length=1)
    type: MinigameParameterType
    required: bool = True
    default: Any | None = None
    description: str = Field(min_length=1)
    allowed_values: list[str] = Field(default_factory=list)
    minimum: float | None = None
    maximum: float | None = None
    min_length: int | None = None
    max_length: int | None = None
    string_mode: MinigameStringMode = MinigameStringMode.TEXT

    @model_validator(mode="after")
    def validate_definition(self) -> "MinigameParameterDefinition":
        if self.type == MinigameParameterType.ENUM and not self.allowed_values:
            raise ValueError(f"Enum parameter '{self.name}' requires allowed_values.")

        if self.minimum is not None and self.maximum is not None and self.minimum > self.maximum:
            raise ValueError(f"Parameter '{self.name}' has minimum greater than maximum.")

        if self.min_length is not None and self.max_length is not None and self.min_length > self.max_length:
            raise ValueError(f"Parameter '{self.name}' has min_length greater than max_length.")

        if self.default is not None:
            _, errors = self.validate_and_normalize(self.default)
            if errors:
                joined = "; ".join(errors)
                raise ValueError(f"Default for parameter '{self.name}' is invalid: {joined}")

        return self

    def validate_and_normalize(self, value: Any) -> tuple[Any, list[str]]:
        if self.type == MinigameParameterType.ENUM:
            return self._validate_enum(value)
        if self.type == MinigameParameterType.INT:
            return self._validate_int(value)
        if self.type == MinigameParameterType.FLOAT:
            return self._validate_float(value)
        if self.type == MinigameParameterType.BOOL:
            return self._validate_bool(value)

        return self._validate_string(value)

    def _validate_enum(self, value: Any) -> tuple[Any, list[str]]:
        if not isinstance(value, str):
            return value, [f"must be a string enum value, got {type(value).__name__}."]
        if value not in self.allowed_values:
            return value, [f"must be one of {self.allowed_values}, got '{value}'."]
        return value, []

    def _validate_int(self, value: Any) -> tuple[Any, list[str]]:
        if isinstance(value, bool) or not isinstance(value, int):
            return value, [f"must be an integer, got {type(value).__name__}."]

        errors = self._validate_numeric_bounds(float(value))
        return value, errors

    def _validate_float(self, value: Any) -> tuple[Any, list[str]]:
        if isinstance(value, bool) or not isinstance(value, (int, float)):
            return value, [f"must be a number, got {type(value).__name__}."]

        normalized = float(value)
        errors = self._validate_numeric_bounds(normalized)
        return normalized, errors

    def _validate_bool(self, value: Any) -> tuple[Any, list[str]]:
        if not isinstance(value, bool):
            return value, [f"must be a boolean, got {type(value).__name__}."]
        return value, []

    def _validate_string(self, value: Any) -> tuple[Any, list[str]]:
        if not isinstance(value, str):
            return value, [f"must be a string, got {type(value).__name__}."]

        errors: list[str] = []
        if self.min_length is not None and len(value) < self.min_length:
            errors.append(f"must be at least {self.min_length} characters long.")
        if self.max_length is not None and len(value) > self.max_length:
            errors.append(f"must be at most {self.max_length} characters long.")
        if self.allowed_values and value not in self.allowed_values:
            errors.append(f"must be one of {self.allowed_values}, got '{value}'.")
        return value, errors

    def _validate_numeric_bounds(self, value: float) -> list[str]:
        errors: list[str] = []
        if self.minimum is not None and value < self.minimum:
            errors.append(f"must be >= {self.minimum:g}, got {value:g}.")
        if self.maximum is not None and value > self.maximum:
            errors.append(f"must be <= {self.maximum:g}, got {value:g}.")
        return errors


class MinigameGeneratorDefinition(BaseModel):
    generator_id: str = Field(min_length=1)
    minigame_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    fit_tags: list[str] = Field(default_factory=list)
    difficulty_bands: list[str] = Field(default_factory=list)
    required_world_state: list[str] = Field(default_factory=list)
    parameter_schema: dict[str, MinigameParameterDefinition]
    defaults: dict[str, Any]
    coupling_rules: list[MinigameCouplingRule] = Field(default_factory=list)
    fallback_generator_ids: list[str] = Field(default_factory=list)
    preview_text_template: str = Field(min_length=1)

    @model_validator(mode="after")
    def validate_definition(self) -> "MinigameGeneratorDefinition":
        if not self.parameter_schema:
            raise ValueError(f"Generator '{self.generator_id}' requires at least one parameter.")
        if not self.fallback_generator_ids:
            raise ValueError(f"Generator '{self.generator_id}' requires at least one fallback generator ID.")

        unknown_default_keys = sorted(set(self.defaults) - set(self.parameter_schema))
        if unknown_default_keys:
            raise ValueError(
                f"Generator '{self.generator_id}' has defaults for unknown parameters: {unknown_default_keys}."
            )

        missing_defaults = [name for name in self.parameter_schema if name not in self.defaults]
        if missing_defaults:
            raise ValueError(f"Generator '{self.generator_id}' is missing defaults for: {missing_defaults}.")

        for name, parameter in self.parameter_schema.items():
            if parameter.name != name:
                raise ValueError(
                    f"Generator '{self.generator_id}' parameter key '{name}' does not match '{parameter.name}'."
                )

            default_value = self.defaults[name]
            if parameter.default != default_value:
                raise ValueError(
                    f"Generator '{self.generator_id}' parameter '{name}' default disagrees with defaults table."
                )

        return self
