# Historias de usuario individuales

**Nombre:** Andres Bedoya

**Usuario de GitHub:** AndBedoya

---

## Mis historias de usuario

> Entre 5 y 7 historias en formato "como [rol] quiero [acción] para [beneficio]", pensadas desde distintos roles o necesidades del producto que el equipo está diseñando. Si escribes menos de 7, borra las líneas que no uses (mínimo 5).

**Componente:** C04 - Estado, blockchain y auditoría

> 1. Como Servicio de emisión quiero registrar un evento auditable de emisión correlacionado con la credencial para conservar evidencia de quién hizo qué, cuándo y con qué resultado.
2. Como Auditor quiero consultar la evidencia blockchain asociada a una credencial para corroborar la existencia de un anclaje sin acceder a datos personales.
3. Como Motor de verificación quiero consultar el estado actual de una credencial para incorporar vigencia al proceso de verificación.
4. Como Emisor autorizado quiero revocar una credencial indicando un motivo para impedir que continúe tratándose como vigente cuando existe una causa válida.
5. Como Emisor autorizado quiero suspender temporalmente una credencial y posteriormente reactivarla cuando la política lo permita para gestionar situaciones reversibles sin usar revocación definitiva.
6. Como Auditor quiero consultar la línea de tiempo de eventos de una credencial para reconstruir su ciclo de vida de manera trazable.
7. Como Auditor quiero verificar la integridad de la cadena o estructura de evidencias de auditoría para detectar alteraciones en los registros técnicos.
8. Como Servicio de emisión quiero registrar localmente una operación pendiente cuando la red blockchain no esté disponible para evitar perder trazabilidad sin bloquear indebidamente todo el proceso.

## La más importante y por qué

> Organiza las historias de mayor a menor importancia: en la primera fila va la más importante. En cada fila indica el número de la historia y por qué la ubicaste en esa posición. Si usaste menos de 7 historias, borra las filas que sobren.

| **Orden de importancia** | **Historia #** | **Por qué** |
| ------------------------ | -------------- | ----------- |
| 1 (la más importante) | #1 | Conserva evidencia auditable de la emisión y correlaciona los eventos del ciclo de vida. |
| 2 | #3 | Aporta el estado vigente requerido por el motor de verificación. |
| 3 | #4 | Permite invalidar de manera gobernada una credencial cuando existe una causa válida. |
| 4 | #7 | Ayuda a detectar alteraciones en la evidencia técnica de auditoría. |
| 5 | #2 | Permite comprobar el anclaje blockchain sin exponer información personal. |
| 6 | #6 | Permite reconstruir la secuencia de eventos de una credencial. |
| 7 | #5 | Gestiona situaciones reversibles sin recurrir a una revocación definitiva. |
| 8 (la menos importante) | #8 | Aumenta la resiliencia frente a indisponibilidad temporal de la red blockchain. |
