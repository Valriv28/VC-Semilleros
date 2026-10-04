# Product Blueprint

**Nombre del proyecto:** Credenciales verificables de asistencia a semilleros

**Repositorio (enlace obligatorio):** [VC-Semilleros](https://github.com/Valriv28/VC-Semilleros.git)

> Los campos marcados como *enlace obligatorio* deben ir como enlace en Markdown, con este formato: `[texto del enlace](https://...)`. Reemplacen el texto y la dirección de ejemplo.

---

## Contenido

1. Priorización de historias
2. Propuesta de valor
3. Flujo de usuario
4. Alcance del MVP
5. Lean Canvas
6. Backlog priorizado (Kanban)
7. Arquitectura inicial
8. Uso de Stellar y justificación

---

## 1. Priorización de historias

> Historias elegidas entre las que propuso el equipo y criterio con que se priorizaron. Son las que pasan al backlog. Extensión: breve.

**Criterio de priorización:** Se aplica MoSCoW. Las historias **Must Have** conforman el recorrido mínimo necesario para registrar evidencia de asistencia, determinar elegibilidad, emitir, entregar, verificar y auditar una credencial. Las **Should Have** fortalecen interoperabilidad, auditoría, resiliencia y gestión de estados sin impedir demostrar el valor central del MVP. Las **Could Have** se conservan en el backlog como evolución posterior porque aportan control de versión y validación federada simulada, pero no son necesarias para probar el flujo principal. En esta etapa no se definieron historias **Won't Have**.

| **Prioridad** | **Historia** | **Propuesta por** | **Por qué entra al backlog** |
| ------------- | ------------ | ----------------- | ---------------------------- |
| **Must Have** · HU-C01-01 | Como Servicio de emisión, quiero construir una credencial verificable de asistencia a semillero a partir de datos autorizados, para obtener una representación interoperable y consistente del reconocimiento. | Estudiante 1 · C01 | Define el artefacto interoperable sobre el que se ejecuta el resto del ciclo de emisión y verificación. |
| **Must Have** · HU-C01-02 | Como Servicio de emisión, quiero validar la conformidad estructural de la credencial antes de firmarla, para impedir la emisión de credenciales incompletas o incompatibles con el perfil adoptado. | Estudiante 1 · C01 | Evita que una credencial incompleta o incompatible avance al proceso de firma. |
| **Must Have** · HU-C01-03 | Como Emisor autorizado, quiero firmar una credencial estructuralmente conforme con el mecanismo criptográfico adoptado, para producir evidencia verificable de autenticidad e integridad. | Estudiante 1 · C01 | Aporta la prueba criptográfica necesaria para comprobar integridad y relación con el emisor. |
| **Must Have** · HU-C01-04 | Como Motor de verificación, quiero verificar la prueba criptográfica de una credencial presentada, para determinar si su contenido conserva integridad y corresponde al emisor declarado. | Estudiante 1 · C01 | Permite validar técnicamente la prueba criptográfica durante la verificación. |
| **Must Have** · HU-C01-05 | Como Servicio de emisión, quiero serializar la credencial en el formato de intercambio definido, para entregarla sin perder significado ni evidencia criptográfica. | Estudiante 1 · C01 | Hace posible entregar e intercambiar la credencial sin perder su semántica ni su prueba. |
| **Must Have** · HU-C02-01 | Como Administrador institucional, quiero registrar un semillero asociado a mi institución, para habilitarlo como contexto gobernado para futuras certificaciones. | Estudiante 2 · C02 | Establece el contexto institucional gobernado desde el cual se originan las certificaciones. |
| **Must Have** · HU-C02-02 | Como Administrador institucional, quiero activar o inactivar un semillero, para controlar si puede originar nuevas certificaciones. | Estudiante 2 · C02 | Controla si un semillero puede generar nuevas operaciones de certificación. |
| **Must Have** · HU-C02-03 | Como Responsable de semillero, quiero crear un periodo de certificación para un semillero activo, para delimitar temporalmente la asistencia que será evaluada. | Estudiante 2 · C02 | Delimita el periodo sobre el cual se acumula y evalúa la evidencia de asistencia. |
| **Must Have** · HU-C02-04 | Como Responsable de semillero, quiero registrar participantes en un periodo, para establecer quiénes pueden acumular evidencia de asistencia. | Estudiante 2 · C02 | Define qué participantes pueden acumular evidencia dentro de un periodo. |
| **Must Have** · HU-C02-05 | Como Responsable de semillero, quiero registrar la asistencia de un participante a las sesiones o actividades del periodo, para conservar evidencia verificable para evaluar posteriormente el criterio. | Estudiante 2 · C02 | Genera la evidencia primaria necesaria para evaluar posteriormente el cumplimiento. |
| **Must Have** · HU-C02-06 | Como Administrador institucional, quiero definir y versionar el criterio de certificación de asistencia, para establecer previamente la regla con la que se decidirá la elegibilidad. | Estudiante 2 · C02 | Fija previamente la regla de certificación y permite trazabilidad de sus versiones. |
| **Must Have** · HU-C02-07 | Como Responsable de semillero, quiero evaluar la evidencia de asistencia de un participante contra el criterio vigente, para determinar de forma reproducible si cumple o no cumple. | Estudiante 2 · C02 | Determina de forma reproducible si la evidencia satisface el criterio vigente. |
| **Must Have** · HU-C02-08 | Como Emisor autorizado, quiero declarar elegible para emisión a un participante que cumple el criterio, para separar explícitamente la decisión de elegibilidad del registro de asistencia. | Estudiante 2 · C02 | Separa explícitamente la elegibilidad del simple registro de asistencia. |
| **Must Have** · HU-C02-09 | Como Emisor autorizado, quiero autorizar y solicitar la emisión de la credencial para un participante elegible, para iniciar una emisión gobernada y trazable. | Estudiante 2 · C02 | Conecta una elegibilidad válida con el inicio gobernado del proceso de emisión. |
| **Must Have** · HU-C02-10 | Como Emisor autorizado, quiero confirmar la emisión después de recibir la credencial firmada y registrar su referencia de auditoría, para cerrar el proceso sin confundir la VC con la evidencia de asistencia o elegibilidad. | Estudiante 2 · C02 | Cierra el ciclo de emisión y conserva su referencia de auditoría. |
| **Must Have** · HU-C03-01 | Como Verificador, quiero presentar una credencial de asistencia para su verificación, para obtener un análisis técnico y de confianza reproducible. | Estudiante 3 · C03 | Constituye el punto de entrada para analizar una credencial presentada. |
| **Must Have** · HU-C03-02 | Como Verificador, quiero conocer si la estructura y prueba criptográfica de la credencial son correctas, para distinguir integridad técnica de confianza institucional. | Estudiante 3 · C03 | Comprueba la conformidad técnica y la integridad criptográfica antes de evaluar confianza. |
| **Must Have** · HU-C03-03 | Como Verificador, quiero comprobar si el emisor estaba autorizado y su institución reconocida al momento de emisión, para evaluar la procedencia de la declaración. | Estudiante 3 · C03 | Permite establecer si la declaración procede de un emisor autorizado. |
| **Must Have** · HU-C03-04 | Como Verificador, quiero consultar el estado vigente de la credencial, para saber si está vigente, suspendida o revocada. | Estudiante 3 · C03 | Incorpora la vigencia actual al resultado de verificación. |
| **Must Have** · HU-C03-05 | Como Verificador, quiero recibir un resultado consolidado y explicable de la verificación, para comprender qué controles pasaron, fallaron o no pudieron evaluarse. | Estudiante 3 · C03 | Consolida los controles en un resultado comprensible y explicable para el verificador. |
| **Must Have** · HU-C04-01 | Como Servicio de emisión, quiero registrar un evento auditable de emisión correlacionado con la credencial, para conservar evidencia de quién hizo qué, cuándo y con qué resultado. | Estudiante 4 · C04 | Conserva evidencia auditable de la emisión y correlaciona los eventos del ciclo de vida. |
| **Must Have** · HU-C04-02 | Como Auditor, quiero consultar la evidencia blockchain asociada a una credencial, para corroborar la existencia de un anclaje sin acceder a datos personales. | Estudiante 4 · C04 | Permite comprobar el anclaje blockchain sin exponer información personal. |
| **Must Have** · HU-C04-03 | Como Motor de verificación, quiero consultar el estado actual de una credencial, para incorporar vigencia al proceso de verificación. | Estudiante 4 · C04 | Aporta el estado vigente requerido por el motor de verificación. |
| **Must Have** · HU-C04-04 | Como Emisor autorizado, quiero revocar una credencial indicando un motivo, para impedir que continúe tratándose como vigente cuando existe una causa válida. | Estudiante 4 · C04 | Permite invalidar de manera gobernada una credencial cuando existe una causa válida. |
| **Must Have** · HU-C04-06 | Como Auditor, quiero consultar la línea de tiempo de eventos de una credencial, para reconstruir su ciclo de vida de manera trazable. | Estudiante 4 · C04 | Permite reconstruir la secuencia de eventos de una credencial. |
| **Must Have** · HU-C04-07 | Como Auditor, quiero verificar la integridad de la cadena o estructura de evidencias de auditoría, para detectar alteraciones en los registros técnicos. | Estudiante 4 · C04 | Ayuda a detectar alteraciones en la evidencia técnica de auditoría. |
| **Must Have** · HU-C05-01 | Como Usuario institucional, quiero autenticarme en el portal y operar según mis roles autorizados, para acceder únicamente a las funciones que me corresponden. | Estudiante 5 · C05 | Aplica el control de acceso y traduce los roles de gobierno en permisos operativos. |
| **Must Have** · HU-C05-02 | Como Administrador institucional, quiero gestionar semilleros y periodos mediante el portal, para operar C02 sin acceder directamente a su almacenamiento. | Estudiante 5 · C05 | Permite administrar semilleros y periodos desde la interfaz sin acoplarla al almacenamiento. |
| **Must Have** · HU-C05-03 | Como Responsable de semillero, quiero registrar y consultar asistencia de participantes, para mantener la evidencia primaria que luego será evaluada. | Estudiante 5 · C05 | Expone el registro de la evidencia primaria de asistencia. |
| **Must Have** · HU-C05-04 | Como Responsable de semillero, quiero consultar el criterio vigente y ejecutar la evaluación de elegibilidad, para conocer quién cumple antes de solicitar una emisión. | Estudiante 5 · C05 | Permite ejecutar la evaluación de elegibilidad antes de solicitar la emisión. |
| **Must Have** · HU-C05-05 | Como Emisor autorizado, quiero solicitar la emisión de la credencial de un participante elegible, para ejecutar el flujo gobernado desde una interfaz controlada. | Estudiante 5 · C05 | Lleva el flujo de emisión gobernada a una interfaz de usuario controlada. |
| **Must Have** · HU-C05-06 | Como Participante, quiero consultar y obtener mi credencial emitida, para disponer de la certificación de asistencia que me fue otorgada. | Estudiante 5 · C05 | Entrega al participante acceso a la credencial que le fue otorgada. |
| **Must Have** · HU-C05-07 | Como Verificador, quiero presentar una credencial y visualizar su resultado de verificación, para comprender su estructura, integridad, emisor y estado. | Estudiante 5 · C05 | Materializa para el verificador el resultado de estructura, integridad, emisor y estado. |
| **Should Have** · HU-C01-06 | Como Motor de verificación, quiero interpretar una credencial de asistencia compatible recibida externamente, para extraer sus afirmaciones y evidencias para la verificación. | Estudiante 1 · C01 | Amplía la interoperabilidad con credenciales compatibles externas sin bloquear el MVP propio. |
| **Should Have** · HU-C03-06 | Como Auditor, quiero contrastar la referencia de integridad/auditoría asociada a la credencial, para detectar inconsistencias entre evidencia registrada y credencial presentada. | Estudiante 3 · C03 | Añade contraste con evidencia de integridad y auditoría registrada. |
| **Should Have** · HU-C03-07 | Como Auditor, quiero consultar el contexto histórico relevante de una credencial, para explicar decisiones de confianza sin alterar el pasado. | Estudiante 3 · C03 | Permite explicar decisiones de confianza desde el contexto histórico sin alterar el pasado. |
| **Should Have** · HU-C04-05 | Como Emisor autorizado, quiero suspender temporalmente una credencial y posteriormente reactivarla cuando la política lo permita, para gestionar situaciones reversibles sin usar revocación definitiva. | Estudiante 4 · C04 | Gestiona situaciones reversibles sin recurrir a una revocación definitiva. |
| **Should Have** · HU-C04-08 | Como Servicio de emisión, quiero registrar localmente la operación pendiente cuando la red blockchain no esté disponible, para evitar perder trazabilidad sin bloquear indebidamente todo el proceso. | Estudiante 4 · C04 | Aumenta la resiliencia frente a indisponibilidad temporal de la red blockchain. |
| **Should Have** · HU-C05-08 | Como Auditor, quiero consultar la trazabilidad de una credencial y su evidencia técnica, para reconstruir su ciclo de vida según mis permisos. | Estudiante 5 · C05 | Expone la trazabilidad al auditor de acuerdo con sus permisos. |
| **Should Have** · HU-C05-09 | Como Administrador técnico, quiero visualizar claramente que los dominios institucionales adicionales usados en pruebas son simulados, para evitar interpretar el prototipo como validación interinstitucional real. | Estudiante 5 · C05 | Hace explícito el alcance experimental de los dominios simulados y evita conclusiones incorrectas. |
| **Could Have** · HU-C01-07 | Como Administrador técnico, quiero identificar la versión del perfil de credencial utilizada, para mantener trazabilidad y evolución controlada del formato. | Estudiante 1 · C01 | Facilita la evolución controlada del perfil, aunque puede implementarse después del flujo central. |
| **Could Have** · HU-C03-08 | Como Administrador técnico, quiero ejecutar verificaciones con al menos dos dominios institucionales simulados, para probar que el modelo distingue autoridades sin afirmar validación interinstitucional real. | Estudiante 3 · C03 | Sirve para probar federación simulada, pero no es necesaria para demostrar el flujo principal. |

*(Agreguen o borren filas según las historias que pasen al backlog.)*

---

## 2. Propuesta de valor

> Qué resultado obtiene el usuario y por qué elegiría esta solución. En qué se diferencia de cómo resuelve hoy. Conecta con el usuario del Problem Brief. Extensión: 150–300 palabras en total.

**Usuario (del Problem Brief):** Participantes de semilleros que necesitan demostrar su asistencia y terceros que requieren comprobar la autenticidad, integridad y vigencia de esa evidencia.

**Resultado que obtiene:** El participante obtiene una credencial digital verificable que representa una decisión de certificación basada en evidencia de asistencia y en un criterio previamente definido. La credencial puede presentarse posteriormente ante un tercero, quien puede revisar su estructura, prueba criptográfica, emisor y estado sin depender de una validación manual caso por caso.

**Por qué elegiría esta solución:** La solución separa explícitamente asistencia, elegibilidad, autorización de emisión, credencial y verificación. Esa separación evita tratar un registro de asistencia como si fuera automáticamente una certificación. Además, mantiene trazabilidad sobre las decisiones y permite obtener un resultado de verificación explicable.

**En qué se diferencia de cómo lo resuelve hoy:** Actualmente la comprobación depende principalmente del emisor y de procesos manuales asociados a certificados o solicitudes de validación. La propuesta incorpora credenciales verificables, pruebas criptográficas, control de estado y evidencia auditable. Blockchain se utiliza como mecanismo complementario de anclaje e integridad, no como repositorio de datos personales ni como sustituto de las reglas de negocio.

---

## 3. Flujo de usuario

> Recorrido de la persona por la solución de principio a fin, roles y puntos de interacción. Diagrama o secuencia numerada. Extensión: 150–300 palabras.

| **Paso** | **Rol** | **Qué hace** | **Punto de interacción** |
| -------- | ------- | ------------ | ------------------------ |
| 1 | Administrador institucional | Registra el semillero, controla su estado y define el contexto institucional de certificación. | Portal web |
| 2 | Responsable de semillero | Crea el periodo, registra participantes y conserva la evidencia de asistencia. | Portal web / API |
| 3 | Administrador institucional | Define y versiona el criterio de certificación aplicable al periodo. | Portal web |
| 4 | Responsable de semillero | Evalúa la evidencia de asistencia frente al criterio vigente. | Portal web / servicio de elegibilidad |
| 5 | Emisor autorizado | Declara la elegibilidad, autoriza la emisión y solicita la generación de la credencial. | Portal web / API |
| 6 | Servicio de emisión | Construye, valida, firma y serializa la credencial verificable. | C01 / servicios internos |
| 7 | Servicio de auditoría | Registra el evento de emisión y la evidencia de integridad correspondiente. | C04 / adaptador Stellar |
| 8 | Participante | Consulta y obtiene la credencial emitida. | Portal web |
| 9 | Verificador | Presenta la credencial y solicita su análisis. | Portal web / API de verificación |
| 10 | Motor de verificación | Comprueba estructura, prueba criptográfica, confianza del emisor y estado; luego produce un resultado explicable. | C01 + C03 + C04 |
| 11 | Auditor | Consulta, cuando tiene autorización, la trazabilidad y evidencia técnica del ciclo de vida. | Portal web / servicios de auditoría |

*(Agreguen los pasos que hagan falta. Si prefieren, inserten aquí un diagrama.)*

---

## 4. Alcance del MVP

> Funcionalidad central separada de la deseable que queda fuera. Justificación de por qué el recorte sigue entregando valor. Extensión: 150–300 palabras en total.

| **Dentro del MVP (funcionalidad central)** | **Fuera del MVP (deseable, para después)** |
| ------------------------------------------ | ------------------------------------------ |
| Registro y gestión básica de semilleros, periodos, participantes y asistencia | Interoperabilidad ampliada con credenciales externas y perfiles adicionales |
| Definición del criterio, evaluación de elegibilidad y autorización de emisión | Versionamiento avanzado y migración de perfiles históricos |
| Construcción, validación, firma y serialización de la credencial | Escenarios federados con múltiples instituciones reales |
| Consulta y entrega de la credencial al participante | Automatizaciones operativas no necesarias para demostrar el flujo principal |
| Verificación de estructura, integridad criptográfica, emisor y estado | Capacidades avanzadas de consulta histórica y explotación analítica |
| Revocación, trazabilidad mínima y evidencia auditable | Suspensión/reactivación y resiliencia ampliada ante indisponibilidad blockchain |
| Anclaje de evidencia mínima en Stellar sin almacenar PII ni la VC completa | Evoluciones del modelo de gobernanza posteriores al prototipo |

**Por qué el recorte sigue entregando valor:** El MVP conserva el recorrido completo que permite demostrar el problema y la solución de extremo a extremo. Existe evidencia de asistencia, una regla explícita de certificación, una decisión de elegibilidad, una emisión gobernada y una credencial que puede ser verificada por un tercero. El recorte deja para después funcionalidades de ampliación, resiliencia e interoperabilidad avanzada, pero no elimina ninguna capacidad necesaria para validar que la credencial fue emitida bajo reglas conocidas, conserva integridad y puede comprobarse posteriormente.

---

## 5. Lean Canvas

> Lienzo de una página con el modelo del producto. Extensión: enlace (obligatorio).

**Enlace al Lean Canvas (obligatorio):** [Lean Canvas del proyecto](https://miro.com/app/board/uXjVEeg5-Xw=/?share_link_id=597889439654)

El lienzo debe cubrir: problema, segmento de usuarios, propuesta de valor única, solución, canales, métricas clave, ventaja diferencial y estructura de costos e ingresos.

---

## 6. Backlog priorizado (Kanban)

> Enlace al tablero en GitHub Projects, construido con las historias priorizadas, en columnas y con criterios de aceptación por tarjeta. Extensión: enlace al tablero (obligatorio).

**Enlace al tablero (obligatorio):** [Tablero Kanban en GitHub Projects](https://github.com/users/Valriv28/projects/2)

---

## 7. Arquitectura inicial

> Cómo se conectan las partes (interfaz, lógica, Stellar) y en qué punto entra la red. Diagrama simple en imagen. Extensión: 150–300 palabras en total.

**Diagrama (imagen o enlace):** Escriban aquí el enlace o inserten la imagen.

| **Capa** | **Componente** | **Qué hace** |
| -------- | -------------- | ------------ |
| Interfaz | VCSemilleros.Maui, VCSemilleros.Web y VCSemilleros.Api | Exponen las capacidades del sistema a participantes, administradores, emisores, verificadores y auditores. Los hosts consumen casos de uso y no acceden directamente a infraestructura o blockchain. |
| Lógica | Application + Domain | Implementa los casos de uso de asistencia, elegibilidad, emisión, verificación, estado y auditoría. El dominio conserva reglas, invariantes y transiciones sin depender de UI, PostgreSQL o Stellar. |
| Persistencia e integración | Adaptadores de salida | Implementan persistencia, identidad, observabilidad, criptografía y demás dependencias externas mediante puertos definidos por el núcleo. |
| Stellar | Adaptador Stellar detrás de un puerto de salida | Registra o consulta evidencia mínima de integridad y auditoría sin almacenar la credencial completa ni datos personales en la red. |

**En qué punto entra la red:** Stellar entra después de que la aplicación ha validado la operación que necesita evidencia externa. El dominio no invoca directamente SDK, RPC ni contratos de Stellar. El caso de uso solicita el anclaje a través de un puerto de salida y el adaptador de infraestructura traduce esa solicitud a una operación sobre la red. Esto mantiene la arquitectura hexagonal, reduce acoplamiento tecnológico y permite sustituir o evolucionar la infraestructura sin modificar las reglas centrales de asistencia, elegibilidad, emisión o verificación.

---

## 8. Uso de Stellar y justificación

> Qué componentes de Stellar usaría y por qué cada uno. Apoyado en el criterio de pertinencia del Problem Brief. Extensión: 150–300 palabras en total.

**Criterio de pertinencia (del Problem Brief):** La verificación no debería depender exclusivamente de consultar al emisor. Se requiere una evidencia técnica que contribuya a comprobar integridad y trazabilidad sin publicar información personal ni almacenar la credencial completa en blockchain.

| **Componente de Stellar** | **Para qué lo usamos** | **Por qué ese y no otra alternativa** |
| ------------------------- | ----------------------- | ------------------------------------ |
| Red Stellar / Testnet durante el prototipo | Registrar una transacción o referencia verificable asociada al anclaje de evidencia de integridad. | Permite realizar el experimento de forma reproducible en una red pública de prueba sin exponer el prototipo directamente a Mainnet. |
| Soroban, si la regla de anclaje requiere lógica programable | Implementar una operación mínima y explícita de registro o consulta de evidencia cuando la simple referencia transaccional no sea suficiente. | Mantiene la lógica on-chain acotada y evita trasladar reglas de negocio completas a blockchain. Si el caso puede resolverse solo con una transacción y referencia verificable, el contrato no debe introducirse por obligación. |
| Hash / identificador de evidencia + referencia de transacción | Vincular la evidencia off-chain con un registro comprobable en Stellar. | Evita almacenar PII o la VC completa en la cadena y permite contrastar integridad con un volumen mínimo de datos públicos. |

Stellar se utiliza como infraestructura complementaria de evidencia, no como fuente única de verdad del producto. La autenticidad e integridad de la credencial dependen principalmente de su estructura, de la prueba criptográfica y de la confianza en el emisor. El anclaje aporta una referencia externa útil para auditoría y detección de inconsistencias. Esta separación también evita que el proyecto convierta blockchain en una dependencia transversal del dominio. La aplicación puede continuar gestionando asistencia, elegibilidad, credenciales y estados mediante sus propios puertos y adaptadores, mientras la integración con Stellar permanece encapsulada en infraestructura.