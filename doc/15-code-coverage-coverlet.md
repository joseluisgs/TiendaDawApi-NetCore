# 15. Code Coverage con Coverlet

El coverage de codigo mide que porcentaje del codigo esta probado por tests.

---

## 1. Metricas de Coverage

```mermaid
flowchart LR
    subgraph "Test Run"
        T[Tests Execution]
    end
    
    subgraph "Coverage Report"
        L["Line: 49.79%"]
        B["Branch: 56.01%"]
        M["Method: 70.73%"]
    end
    
    subgraph "Thresholds"
        A[Target: 70%]
    end
    
    T -->|Coverlet| L
    T --> B
    T --> M
    L -.->|Below| A
```

---

## 2. Instalacion

```bash
dotnet add package coverlet.collector
dotnet add package coverlet.msbuild
```

---

## 2. Configuracion en .csproj

```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverageThreshold>0</CoverageThreshold>
  <CoverletOutputFormat>json,lcov,opencover</CoverletOutputFormat>
  <CoverletOutput>./coverage/</CoverletOutput>
</PropertyGroup>
```

---

## 3. Ejecutar Tests con Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## 4. Reporte de Coverage

```
+-----------+--------+--------+--------+
| Module    | Line   | Branch | Method |
+-----------+--------+--------+--------+
| TiendaApi | 49.79% | 56.01% | 70.73% |
+-----------+--------+--------+--------+
```

---

## 5. Interpretar Coverage

| Coverage | Color | Significado |
|----------|-------|-------------|
| < 50% | Rojo | Insuficiente |
| 50-70% | Amarillo | Aceptable |
| 70-90% | Verde | Bueno |
| > 90% | Verde oscuro | Excelente |

---

## 6. Beneficios

- **Calidad**: Identificar codigo no probado
- **Confianza**: Mayor cobertura, mayor confianza
- **Mantenibilidad**: Facilita refactorizacion
- **Estandares**: Cumple requisitos de calidad
