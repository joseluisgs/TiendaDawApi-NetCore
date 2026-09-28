# Plantilla de ADR (Architecture Decision Record)

Documento de ejemplo para registrar una decisión de arquitectura del proyecto.
Copia este fichero con el siguiente número de secuencia (`NN-titulo-corto.md`)
y rellena cada sección. Mantén cada ADR en un solo archivo: corto, legible y
en español.

---

|               |                                                                        |
| ------------- | ---------------------------------------------------------------------- |
| **ADR**       | `ADR-00NN` (número correlativo dentro del título)                       |
| **Título**    | Título corto y descriptivo de la decisión                               |
| **Estado**    | Propuesto · Aceptado · Rechazado · Obsoleto · Sustituido por otro ADR   |
| **Fecha**     | `AAAA-MM` en el que se aceptó                                           |
| **Decisores** | Quién participa en la decisión (equipo docente/alumnado)                |

## Contexto

¿Qué problema o fuerza obliga a decidir? Describe el escenario real sin
anticipar la solución: restricciones técnicas, requisitos funcionales,
limitaciones de equipo o de despliegue.

## Decisión

La decisión tomada, en modo indicativo y con el alcance exacto. Si la
decisión incluye reglas o límites, enuméralos. Debe poder entenderse sin leer
el código.

## Consecuencias

### Positivas

- Beneficios esperados de la decisión.

### Negativas o riesgos

- Costes, deudas o limitaciones que asumimos conscientemente.

## Alternativas descartadas

| Alternativa                 | Por qué se descartó                  |
| --------------------------- | ------------------------------------ |
| Opción A                    | Motivo concreto (técnico o de equipo) |
| Opción B                    | Motivo concreto                      |

## Verificación

¿Cómo sabemos que la decisión se cumple? (tests, comprobaciones automáticas,
revisión manual, etc.)

## Referencias en el código y en la documentación

- `ruta/al/fichero.cs` — donde se implementa.
- `doc/NN-tema.md` — explicación didáctica del tema.
