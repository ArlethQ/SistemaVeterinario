# Configuración externa y rotación de credenciales

## Credenciales que deben rotarse

- Usuario y contraseña de SQL Server presentes anteriormente en configuración versionada.
- Credenciales de base de datos incluidas en `Web.config` del sistema WebForms, incluso las comentadas.
- Credenciales SMTP y OAuth históricas del sistema WebForms.
- Secreto de aplicación usado para envío por Microsoft Graph.
- Tokens, claves o usuarios de integración E-FACT.

La rotación debe realizarla el responsable de cada ambiente. No se intentó cambiar credenciales externas ni ejecutar operaciones contra producción.

## Configuración requerida

El repositorio mantiene vacíos los valores sensibles. Configurarlos mediante variables de entorno, Secret Manager o `appsettings.Local.json`, que está excluido de Git.

Claves esperadas:

- `ConnectionStrings__VeterinariaConnection`
- `GraphMail__TenantId`
- `GraphMail__ClientId`
- `GraphMail__ClientSecret`
- `GraphMail__SenderEmail`
- Configuración sensible de E-FACT según el ambiente autorizado.

No copiar secretos del sistema legado. Usar credenciales nuevas después de completar la rotación.

## Estado de autenticación

- Sesión anónima por defecto y rutas internas protegidas antes de crear la página solicitada.
- Expiración por inactividad: 30 minutos.
- Límite de inicio de sesión: 5 intentos por usuario en una ventana de 15 minutos.
- Autorregistro deshabilitado: el legado administra cuentas desde el módulo de usuarios.
- Recuperación automática deshabilitada hasta disponer de correo configurable y almacenamiento seguro para tokens de un solo uso.
- El hash SHA-1 histórico sigue siendo una deuda de compatibilidad. No se cambió el esquema ni se inventó una columna para hashes modernos sin validar la base de pruebas.
