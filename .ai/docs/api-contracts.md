# API Contracts — FarmSim VR

## External Integrations
(none yet — this section will be populated when online features are added)

## Internal Contracts
All internal system communication goes through interfaces defined in
Assets/_Project/Scripts/Interfaces/.

## Contract Rules
1. Interfaces define WHAT, not HOW
2. Core/ depends on interfaces, never on implementations
3. MonoBehaviours implement interfaces
4. ScriptableObjects provide data, not behavior
5. New systems must define their interface before implementation
