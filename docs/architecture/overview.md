# Architecture Overview

Diyarak is a modular monolith. Business modules remain physically isolated and communicate through explicit contracts, application orchestration, or events. Foundation packages contain only stable cross-cutting abstractions.

Application projects coordinate use cases that cross module boundaries. Domain rules remain in Modules or Core, Integrations provide infrastructure implementations, and Hosts compose Application use cases with concrete Integration adapters.

    Hosts
    ├── Application → Modules / approved Core contracts → Foundation
    └── Integrations → Application-owned ports

Dependencies follow the rules in `dependency-rules.md`. Modules do not depend on Application or other Module implementations, and Application does not depend on concrete Integrations or Hosts.

Foundation packages contain no database, HTTP, framework, messaging, or cloud dependencies.
