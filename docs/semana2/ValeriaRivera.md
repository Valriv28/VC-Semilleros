# Historias de usuario individuales

**Nombre:** Valeria Rivera Uribe

**Usuario de GitHub:** Valriv28

---

## Mis historias de usuario

> Entre 5 y 7 historias en formato "como [rol] quiero [acción] para [beneficio]", pensadas desde distintos roles o necesidades del producto que el equipo está diseñando. Si escribes menos de 7, borra las líneas que no uses (mínimo 5).

**Componente:** C01 - Núcleo de interoperabilidad W3C VC y Open Badges 3.0
1. Como Servicio de emisión quiero construir una credencial verificable de asistencia a semillero a partir de datos autorizados para obtener una representación interoperable y consistente del reconocimiento.
2. Como Servicio de emisión quiero validar la conformidad estructural de la credencial antes de firmarla para impedir la emisión de credenciales incompletas o incompatibles con el perfil adoptado.
3. Como Emisor autorizado quiero firmar una credencial estructuralmente conforme con el mecanismo criptográfico adoptado para producir evidencia verificable de autenticidad e integridad.
4. Como Motor de verificación quiero verificar la prueba criptográfica de una credencial presentada para determinar si su contenido conserva integridad y corresponde al emisor declarado.
5. Como Servicio de emisión quiero serializar la credencial en el formato de intercambio definido para entregarla sin perder significado ni evidencia criptográfica.
6. Como Motor de verificación quiero interpretar una credencial de asistencia compatible recibida externamente para extraer sus afirmaciones y evidencias para la verificación.
7. Como Administrador técnico quiero identificar la versión del perfil de credencial utilizada para mantener trazabilidad y evolución controlada del formato.

## La más importante y por qué

> Organiza las historias de mayor a menor importancia: en la primera fila va la más importante. En cada fila indica el número de la historia y por qué la ubicaste en esa posición. Si usaste menos de 7 historias, borra las filas que sobren.

| **Orden de importancia** | **Historia #** | **Por qué** |
| ------------------------ | -------------- | ----------- |
| 1 (la más importante) | #1 | Define el artefacto interoperable sobre el que se ejecuta el resto del ciclo de emisión y verificación. |
| 2 | #2 | Evita que una credencial incompleta o incompatible avance al proceso de firma. |
| 3 | #3 | Aporta la prueba criptográfica necesaria para comprobar integridad y relación con el emisor. |
| 4 | #4 | Permite validar técnicamente la prueba criptográfica durante la verificación. |
| 5 | #5 | Hace posible entregar e intercambiar la credencial sin perder su semántica ni su prueba. |
| 6 | #6 | Amplía la interoperabilidad con credenciales externas compatibles. |
| 7 (la menos importante) | #7 | Facilita la evolución controlada del perfil, aunque no es crítica para el flujo inicial. |
