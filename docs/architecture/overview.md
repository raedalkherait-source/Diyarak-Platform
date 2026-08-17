# Architecture Overview

Diyarak begins as a modular monolith. Business modules remain physically isolated and communicate through explicit contracts or events. Foundation packages contain only stable cross-cutting abstractions.

```text
Hosts → Modules/Core → Foundation
```

The Foundation milestone intentionally contains no database, HTTP, framework, messaging, or cloud dependencies.
