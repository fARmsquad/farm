from __future__ import annotations

import json
from typing import TypeVar

import httpx
from pydantic import BaseModel

T = TypeVar("T", bound=BaseModel)


class OpenAIStructuredOutputClient:
    def __init__(
        self,
        *,
        api_key: str,
        model: str,
        timeout_seconds: float = 45.0,
        base_url: str = "https://api.openai.com/v1/responses",
    ) -> None:
        self._api_key = api_key
        self._model = model
        self._timeout_seconds = timeout_seconds
        self._base_url = base_url

    def generate(
        self,
        *,
        response_model: type[T],
        schema_name: str,
        system_prompt: str,
        user_prompt: str,
    ) -> T:
        if not self._api_key:
            raise RuntimeError("OpenAI API key is not configured for structured output generation.")
        if not self._model:
            raise RuntimeError("OpenAI narrative model is not configured.")

        payload = {
            "model": self._model,
            "store": False,
            "input": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
            "text": {
                "format": {
                    "type": "json_schema",
                    "name": schema_name,
                    "strict": True,
                    "schema": response_model.model_json_schema(),
                }
            },
        }

        response = httpx.post(
            self._base_url,
            headers={
                "Authorization": f"Bearer {self._api_key}",
                "Content-Type": "application/json",
            },
            json=payload,
            timeout=self._timeout_seconds,
        )
        response.raise_for_status()
        body = response.json()

        if body.get("error"):
            raise RuntimeError(f"OpenAI structured output request failed: {body['error']}")
        if body.get("status") == "incomplete":
            raise RuntimeError(f"OpenAI structured output request was incomplete: {body.get('incomplete_details')}")

        output_text = _extract_output_text(body)
        return response_model.model_validate(json.loads(output_text))


def _extract_output_text(body: dict) -> str:
    for item in body.get("output", []):
        if item.get("type") != "message":
            continue
        for content in item.get("content", []):
            if content.get("type") == "refusal":
                raise RuntimeError(f"OpenAI structured output request was refused: {content.get('refusal', '')}")
            if content.get("type") == "output_text":
                text = content.get("text")
                if isinstance(text, str) and text.strip():
                    return text
    raise RuntimeError("OpenAI structured output response did not contain output text.")
