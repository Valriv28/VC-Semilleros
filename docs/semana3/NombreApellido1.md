# Entregable 3 — Functional Proof

## 1. Front construido

**Estado de integración: pendiente de verificación.** Andres

**Información por integrar desde el responsable del frontend:**  Juan

## 2. Decisión técnica

**Opción elegida: B. Solución existente del ecosistema Stellar, sin contrato propio.**

Para el desarrollo de VC Semilleros se ha decidido utilizar las capacidades nativas de Stellar Testnet, sin implementar contratos inteligentes propios durante esta etapa. Esta decisión responde al alcance del MVP definido en el Product Blueprint, cuyo propósito es permitir la emisión y verificación de credenciales académicas a partir de los registros de asistencia y del cumplimiento de los criterios establecidos para los participantes de los semilleros de investigación.

La solución se desarrollará utilizando C# como lenguaje de programación, .NET como plataforma de desarrollo, Blazor para la construcción de la interfaz web y ASP.NET Core para los servicios del backend. La arquitectura estará organizada en componentes responsables de la interoperabilidad de credenciales bajo estándares W3C, la gestión de asistencia y emisión, la verificación y confianza, y el control de estados y auditoría. Para la persistencia de la información se utilizará PostgreSQL, donde se almacenarán los registros de asistencia, las solicitudes, las credenciales y demás datos necesarios para el funcionamiento del sistema.

En esta arquitectura, Stellar Testnet se utilizará como mecanismo de anclaje criptográfico para registrar evidencias que permitan comprobar la integridad de la información y fortalecer su trazabilidad. La integración se realizará mediante un adaptador de infraestructura desarrollado en C#, encargado de establecer la comunicación con la red Stellar. Esta separación permitirá mantener la lógica de negocio independiente de la tecnología blockchain.

Las credenciales, los datos personales, las reglas de elegibilidad y los registros operativos permanecerán en PostgreSQL, mientras que en Stellar se registrarán evidencias criptográficas, sin almacenar documentos completos ni información personal. El formato específico de estas evidencias se definirá durante la implementación.

Se descarta, por ahora, el desarrollo de contratos inteligentes propios con Soroban, debido a que las funcionalidades previstas para el MVP no requieren ejecutar reglas de negocio directamente en blockchain. Su incorporación supondría mayor complejidad de desarrollo, mantenimiento y validación de seguridad, sin una necesidad funcional que lo justifique en esta etapa.

Esta decisión permite concentrar los esfuerzos en los procesos de asistencia, elegibilidad, emisión y verificación, aprovechando las capacidades existentes del ecosistema Stellar. La alternativa podrá reconsiderarse si posteriormente surgen requisitos que justifiquen el uso de contratos inteligentes.

## 3. Participación del equipo

| Integrante / GitHub | Componente | Aporte documental propuesto | 
|---|---|---|
| Andres Bedoya | C01 | Perfil de credenciales y responsabilidades de firma e interoperabilidad | 
| Juan Pablo Acevedo | C02 | Flujo de asistencia, elegibilidad y autorización | 
| Alexandra Guerrero | C03 | Verificación compuesta y límites del anclaje blockchain | 
| Isabella Bohada | C04 | Decisión Stellar Testnet, trazabilidad y auditoría | 
| Valeria Rivera | C05 / coordinación | Inventario del portal, README y consolidación de documentación | 


## 4. Bloqueos y siguiente paso


