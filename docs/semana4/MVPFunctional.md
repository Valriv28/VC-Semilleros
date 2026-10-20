# Cierre del proyecto — VC-Semilleros

**Equipo:** Valeria Rivera (coordinación y análisis) · Juan Pablo Acevedo (credenciales verificables) · Isabela Bohada (blockchain y seguridad) · Andrés Bedoya (desarrollo y UX) · Alexandra Guerrero (arquitectura).

**Repositorio:** [VC-Semilleros](https://github.com/Valriv28/VC-Semilleros/tree/main)

> **Nota sobre despliegue y arquitectura:** el producto **no está desplegado en un entorno público**. La verificación se realiza ejecutando el repositorio localmente según el README y contrastando el flujo con el video público y la transacción en Stellar testnet. Adicionalmente, aunque el Product Blueprint describe una arquitectura hexagonal, la implementación final convergió a una **arquitectura en capas** (Api/Front, Application, Domain, Infrastructure), manteniendo la integración con Stellar aislada en su propio proyecto (`Infrastructure.Stellar`).

---

## 1. Aprendizajes y ajustes priorizados


**Aprendizajes.**
1. **La integración toma más tiempo del que se planea:** Subestimamos seriamente el tiempo y el esfuerzo que requiere conectar todas las piezas del sistema. Ajustar la capa de infraestructura, los flujos de datos y la lógica de Stellar tomó más horas de las previstas, lo que nos enseñó a dimensionar mejor los tiempos de integración en futuros proyectos.
2. **Los verificadores no necesitan toda la data, solo el hash:** Descubrimos que para el tercero que recibe el certificado, la privacidad es clave. No necesitan ver el expediente completo del estudiante ni la credencial entera en la red; con validar que el hash de su documento coincide con el anclaje criptográfico, les basta para confiar en la autenticidad.
3. **Blockchain no es una base de datos:** Entendimos que intentar almacenar la información o el certificado completo en la blockchain es un error conceptual. El enfoque correcto (y el que finalmente aplicamos) es usar Stellar únicamente como un ancla de integridad, manteniendo los datos personales y el documento fuera de la red.


**Ajustes ya aplicados antes del cierre.**
- **Enfoque de Stellar:** Reorientamos la integración para usar la red exclusivamente como mecanismo de anclaje de evidencia, garantizando que no se suba información personal ni la credencial completa.
- **Flujo de verificación:** Simplificamos lo que el verificador externo tiene que ver, centrando la interfaz en la validación del hash y el estado de la credencial.
- **Separación de capas:** Aislamos la lógica de Stellar en su propio proyecto de infraestructura (`VCSemilleros.Infrastructure.Stellar`), lo que evitó que un retraso en la red bloqueara el resto de la aplicación.

**Ajustes pendientes (no bloquean el cierre).**
- Actualizar el Product Blueprint para que refleje la arquitectura en capas, evitando contradicciones documentales.
- Suspensión/reactivación y resiliencia ante indisponibilidad de la red (Should Have).
- Interoperabilidad con credenciales externas (Should Have).

**Priorización impacto/esfuerzo:** primero los invariantes de diseño (PII, separación de Stellar); después el recorte MoSCoW; al final las capacidades Should/Could que amplían sin bloquear el valor central.

---

## 2. Riesgos y pendientes

**Riesgos identificados antes del cierre.**

1. **Deriva documental en la arquitectura.** El Blueprint describe hexagonal y el código usa capas. 
2. **Publicación de datos personales on-chain.** Riesgo de exponer PII del estudiante en una red pública. 
3. **El registro de asistencia original puede ser incorrecto.** Blockchain prueba integridad de lo anclado, no la veracidad de la asistencia. 
4. **Usabilidad para verificadores no técnicos.** Un mecanismo criptográfico complejo puede hacer más difícil la consulta que el proceso actual. 
5. **Dependencia de Stellar testnet.** Indisponibilidad o cambios de red durante la demo. 


- **Video público** demostrando el flujo end-to-end: [Video](https://youtu.be/sxlGqYURlUY). 
- **Transacción**: [Transacción](https://stellar.expert/explorer/testnet/tx/ca54012f1c477aca1f96ffa8a4bbd10fcb43a6e5231846cc75c8fc1df8168684).

---

## 3. Plan de validación con usuarios

**A quiénes.** Se validará con los tres perfiles del Problem Brief:
- **Participantes de semilleros** (usuario principal): necesitan demostrar asistencia y conservar su credencial.
- **Tutores y área de Semilleros de Investigación** (operadores): registran asistencia, gestionan periodos y solicitan emisión.
- **Verificadores externos** (otra institución, convocatoria o tercero): necesitan comprobar autenticidad, integridad y vigencia sin contactar al emisor.

**Con qué métodos.**
- **Entrevistas semiestructuradas** (20–30 min) con cada perfil para recoger expectativas y fricciones del proceso actual.
- **Pruebas de usabilidad guiadas** sobre el repositorio ejecutado localmente (o sobre el video si el entorno no está disponible), con tareas concretas: emitir una credencial, presentarla, verificarla y consultar su estado.
- **Registro de métricas de tarea**: tiempo por verificación, tasa de éxito sin ayuda, errores observados.
- **Cuestionario breve post-tarea** (escala 1–5) sobre claridad y confianza en el resultado de verificación.
- **Contraste con la transacción en Stellar testnet** para que el verificador compruebe el anclaje de forma independiente.

**Hipótesis a comprobar.**
- **H1 — Necesidad real:** existe demanda de verificación de certificados después de su emisión. *Criterio: ≥70% de los entrevistados reporta al menos un caso concreto de verificación post-emisión en los últimos 12 meses.*
- **H2 — Valor de verificar sin contactar al emisor:** el verificador valora poder comprobar la credencial sin solicitar validación manual. *Criterio: ≥70% declara que preferiría este mecanismo al actual.*
- **H3 — Usabilidad para no técnicos:** un verificador sin formación técnica completa la verificación sin ayuda. *Criterio: ≥80% de éxito sin asistencia y ≤2 minutos por verificación.*
- **H4 — Claridad del resultado:** el resultado consolidado es comprensible y explicable. *Criterio: ≥4/5 en claridad percibida.*

**Muestra y timeline (propuesta).** 5 participantes + 3 verificadores externos + 2 tutores/área de Semilleros, en un ciclo de 2–3 semanas: semana 1 entrevistas, semana 2 pruebas de usabilidad, semana 3 análisis y ajustes.

**Cómo sabremos que se cumple.** Se considerará validada la propuesta si H1, H2 y H3 se cumplen simultáneamente con los criterios anteriores. Si H1 no se cumple, se revisará la justificación de blockchain; si H3 falla, se priorizará rediseño de la interfaz de verificación antes de ampliar alcance. Los resultados se documentarán en el repositorio con trazabilidad de cada sesión (sin datos personales).

---
## 4. Evidencia verificable

> **Nota:** el producto **no está desplegado públicamente**. La verificación se realiza ejecutando el repositorio localmente según el README y contrastando el flujo con el video y la transacción en Stellar testnet.

| Evidencia | Enlace |
|---|---|
| Repositorio del proyecto | [VC-Semilleros](https://github.com/Valriv28/VC-Semilleros/tree/main) |
| Video público (flujo end-to-end) | [Video](https://youtu.be/sxlGqYURlUY).  |
| Transacción en Stellar testnet (anclaje de evidencia) | [Ver en stellar.expert](https://stellar.expert/explorer/testnet/tx/ca54012f1c477aca1f96ffa8a4bbd10fcb43a6e5231846cc75c8fc1df8168684) |
---
