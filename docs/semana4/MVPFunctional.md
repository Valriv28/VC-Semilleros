# Cierre del MVP | VC-Semilleros

**Programa** Blockchain Builders 101  
**Proyecto** VC-Semilleros  
**Equipo** Valeria Rivera (coordinación y análisis), Juan Pablo Acevedo (credenciales verificables), Isabela Bohada (blockchain y seguridad), Andrés Bedoya (desarrollo y UX) y Alexandra Guerrero (arquitectura) 

**Repositorio** [VC-Semilleros](https://github.com/Valriv28/VC-Semilleros/tree/main)

El MVP se orienta a la emisión y verificación de credenciales académicas asociadas a la participación en semilleros de investigación. Stellar Testnet se emplea para registrar evidencia criptográfica de integridad, mientras las credenciales y los datos personales permanecen fuera de la red. La solución no cuenta con un despliegue público documentado. Las evidencias del cierre corresponden al código disponible, una demostración en video y una transacción registrada en Testnet.

---

## 1. Aprendizajes y ajustes priorizados


**Aprendizajes.**
El desarrollo evidenció que la mayor complejidad no estaba en cada componente por separado, sino en la integración entre el registro de asistencia, la emisión de credenciales, la verificación y Stellar. Este aprendizaje permitió delimitar mejor el alcance del MVP y concentrar el esfuerzo en el flujo principal.

Otra decisión relevante fue utilizar blockchain como mecanismo de anclaje, no como repositorio de certificados. La comparación del hash permite comprobar integridad, pero no acredita por sí sola la identidad del emisor, su autorización ni la vigencia de una credencial. Esta distinción debe mantenerse tanto en la interfaz como en la documentación.


**Ajustes ya aplicados antes del cierre.**
| Prioridad | Ajuste | Impacto | Esfuerzo |
|---|---|---|---|
| 1 | Limitar el uso de Stellar al anclaje de evidencia, sin registrar credenciales completas ni datos personales | Alto | Medio |
| 2 | Simplificar la consulta de verificación y presentar los resultados sin atribuir al hash controles que no ejecuta | Alto | Medio |
| 3 | Organizar la solución en capas y aislar la conexión con Stellar en `VCSemilleros.Infrastructure.Stellar` | Medio | Medio |

**Ajustes pendientes (no bloquean el cierre).**
| Prioridad | Ajuste | Impacto | Esfuerzo |
|---|---|---|---|
| 1 | Actualizar el Product Blueprint y los diagramas conforme a la arquitectura implementada | Alto | Bajo |
| 2 | Completar y probar la gestión de estados y la respuesta ante fallas de Testnet | Alto | Alto |
| 3 | Evaluar la interoperabilidad con credenciales emitidas por otras instituciones | Medio | Alto |

El cierre conserva el alcance funcional alcanzado, identifica las capacidades que requieren validación adicional y evita presentar funcionalidades pendientes como resultados terminados.
  
---

## 2. Riesgos y pendientes

**Riesgos identificados antes del cierre.**

El cierre del MVP permitió identificar aspectos que requieren seguimiento para avanzar hacia una implementación institucional. Los riesgos se presentan según su relación con el alcance demostrado, las decisiones de diseño adoptadas y las actividades previstas para una siguiente etapa.

**Protección de datos académicos.** La exposición de información personal en una blockchain pública constituye un riesgo relevante. Para reducirlo, la arquitectura plantea conservar las credenciales y los datos de los participantes fuera de Stellar y utilizar Testnet como mecanismo de anclaje criptográfico. Antes de un despliegue institucional será necesario revisar los campos de las transacciones y verificar que no revelen información identificable.

**Confiabilidad de la asistencia.** La blockchain permite contrastar la integridad de una evidencia registrada, pero no garantiza que la asistencia de origen sea correcta. Por ello, el proceso de emisión debe apoyarse en reglas de validación y en responsables definidos. Las pruebas adicionales sobre correcciones y trazabilidad de registros se consideran parte del fortalecimiento posterior del sistema.

**Seguridad y alcance de la verificación.** La comprobación del hash permite detectar alteraciones respecto de la evidencia anclada, pero no equivale por sí sola a verificar firma digital, legitimidad del emisor o vigencia. El cierre debe indicar con precisión cuáles de estas comprobaciones ejecuta la aplicación. La revisión de la custodia de claves y de los controles de verificación será prioritaria antes de operar con credenciales reales.

**Dependencia de Stellar Testnet.** La red de pruebas permite demostrar el anclaje sin utilizar una infraestructura de producción, aunque puede presentar interrupciones o cambios de estado. La evidencia de la transacción y el video público facilitan la revisión del resultado obtenido. Para una versión posterior se contemplan pruebas de recuperación, reintentos y conciliación de operaciones.

**Consistencia documental y experiencia de uso.** La implementación se organizó en capas, mientras que el Blueprint inicial planteaba una arquitectura hexagonal. La actualización de los diagramas y de la decisión arquitectónica queda identificada como ajuste documental. Asimismo, las pruebas con participantes, tutores y verificadores externos permitirán comprobar si la consulta resulta clara para usuarios no técnicos.

Estos puntos no se presentan como funcionalidades concluidas. El cierre corresponde al alcance demostrado del MVP; la interoperabilidad con otras instituciones, la gestión avanzada de estados y las pruebas de operación institucional se mantienen como líneas de evolución sujetas a priorización y validación. 


- **Video público** demostrando el flujo end-to-end: [Video](https://youtu.be/sxlGqYURlUY). 
- **Transacción**: [Transacción](https://stellar.expert/explorer/testnet/tx/ca54012f1c477aca1f96ffa8a4bbd10fcb43a6e5231846cc75c8fc1df8168684).

---

## 3. Plan de validación con usuarios

La validación buscará establecer si VC Semilleros responde a una necesidad concreta y si sus funciones principales pueden utilizarse sin conocimientos de blockchain. El ejercicio se desarrollará con los tres perfiles contemplados en el Problem Brief. Los **participantes de semilleros** presentarán sus credenciales; los **tutores y responsables de semilleros** evaluarán el registro de asistencia y la emisión; los **verificadores externos** comprobarán la claridad y utilidad de la consulta.

Se propone una muestra exploratoria de **cinco participantes, dos tutores o responsables y tres verificadores externos**. Este tamaño permite detectar dificultades iniciales, aunque no sustenta conclusiones generalizables sobre todas las instituciones.

La evaluación combinará entrevistas semiestructuradas de 20 a 30 minutos, pruebas de uso moderadas y un cuestionario breve con escala de 1 a 5. Las tareas se realizarán sobre una instancia funcional ejecutada localmente, siempre que esté disponible. El video demostrativo servirá como material de apoyo, pero no reemplazará la interacción directa con el producto. También se contrastará el anclaje con la transacción publicada en Stellar Testnet.

**Hipótesis a comprobar.**
- **H1 — Necesidad real:** existe demanda de verificación de certificados después de su emisión. *Criterio: ≥70% de los entrevistados reporta al menos un caso concreto de verificación post-emisión en los últimos 12 meses.*
- **H2 — Valor de verificar sin contactar al emisor:** el verificador valora poder comprobar la credencial sin solicitar validación manual. *Criterio: ≥70% declara que preferiría este mecanismo al actual.*
- **H3 — Usabilidad para no técnicos:** un verificador sin formación técnica completa la verificación sin ayuda. *Criterio: ≥80% de éxito sin asistencia y ≤2 minutos por verificación.*
- **H4 — Claridad del resultado:** el resultado consolidado es comprensible y explicable. *Criterio: ≥4/5 en claridad percibida.*

Durante la **primera semana** se aplicarán entrevistas y se prepararán las tareas. En la **segunda** se realizarán las pruebas, con registro de tiempos, errores y solicitudes de ayuda. La **tercera semana** se dedicará al análisis de resultados y a la priorización de mejoras.

Los umbrales establecidos son criterios de evaluación propuestos, no resultados obtenidos. Si la necesidad de uso no se confirma, deberá revisarse la propuesta de valor. Si aparecen dificultades de ejecución o comprensión, tendrán prioridad los ajustes en el flujo y en la presentación del resultado. Los hallazgos se documentarán de manera agregada, sin publicar datos personales de los participantes.


---
## 4. Evidencia verificable

El MVP no tiene un despliegue público documentado. Los enlaces disponibles permiten revisar el trabajo desarrollado desde perspectivas complementarias, pero no constituyen por separado una prueba de funcionamiento integral.

| Evidencia | Enlace | Alcance de la comprobación |
|---|---|---|
| Repositorio y documentación | [VC Semilleros en GitHub](https://github.com/Valriv28/VC-Semilleros/tree/main) | Permite examinar el código, su estructura y las instrucciones disponibles para la ejecución local |
| Demostración del MVP | [Video público](https://youtu.be/sxlGqYURlUY) | Presenta el flujo mostrado durante la demostración del equipo |
| Registro en blockchain | [Transacción en Stellar Testnet](https://stellar.expert/explorer/testnet/tx/ca54012f1c477aca1f96ffa8a4bbd10fcb43a6e5231846cc75c8fc1df8168684) | Permite consultar la operación registrada en la red de pruebas |

Para reproducir la demostración, un evaluador deberá poder acceder al repositorio, seguir las instrucciones del README, ejecutar la aplicación localmente y localizar la transacción en el explorador de Stellar. La correspondencia entre una credencial y su anclaje requiere, además, disponer del procedimiento utilizado para generar y contrastar el hash, sin exponer información personal o claves privadas.

**Comprobaciones previas a la entrega.** Verificar el acceso público a los tres enlaces, la suficiencia de las instrucciones de ejecución y la correspondencia entre la transacción y el flujo demostrado. El MVP utiliza funciones nativas de Stellar Testnet y no declara contratos inteligentes propios.

---
