# 14. Manual de usuario por rol

SentryGuard es la plataforma de CyberOps Chile SpA para registrar, seguir y analizar incidentes de ciberseguridad de sus clientes. Todos los datos de este manual son **sintéticos**.

## Cómo entrar

1. Abre la aplicación (con `docker compose up`, en `http://localhost:8088`).
2. Escribe tu correo y tu contraseña y pulsa **Ingresar**.
3. Si te equivocas 5 veces seguidas, la cuenta se bloquea 15 minutos. Es una medida de seguridad.
4. Tu sesión dura 60 minutos; después vuelves a la pantalla de ingreso.
5. Para salir, pulsa **Cerrar sesión** en el menú.

Usuarios de prueba (uno por rol, contraseña `Admin123!`): `admin@sentryguard.cl`, `supervisor@sentryguard.cl`, `analista@sentryguard.cl` y `cliente@sentryguard.cl`. **Deben cambiarse o desactivarse antes de un uso real.**

**Usar la aplicación en el celular:** el menú aparece como botón **Menú** arriba. **Solo con teclado:** el primer `Tab` lleva a «Saltar al contenido principal»; `Escape` cierra los diálogos.

## Qué puede hacer cada rol

| Acción | Administrador | Supervisor | Analista | Cliente |
|---|:-:|:-:|:-:|:-:|
| Ver el dashboard | Todo | Todo | Solo sus incidentes asignados | Solo su organización |
| Ver y exportar reportes | ✅ | ✅ | ❌ | Solo su organización |
| «Resumir con IA» en el dashboard | ✅ | ✅ | ❌ | ❌ |
| Ver incidentes | Todos | Todos | Los asignados | Los de su organización |
| Crear incidentes (con sugerencia de IA) | ✅ | ✅ | ❌ | ✅ |
| Editar un incidente, cambiar su estado, asignarlo | ✅ | ✅ | ❌ | ❌ |
| Eliminar un incidente | ✅ | ❌ | ❌ | ❌ |
| Escribir en la bitácora y subir evidencias | ✅ | ✅ | Solo en los asignados | ❌ |
| Activos: ver | ✅ | ✅ | Los de sus incidentes | Los de su organización |
| Activos: crear y editar | ✅ | ✅ | ❌ | ❌ |
| Activos: eliminar | ✅ | ❌ | ❌ | ❌ |
| Organizaciones y contratos SLA | Ver y administrar | Ver | ❌ | Ver la propia («Mi organización») |
| Usuarios y catálogos | ✅ | ❌ | ❌ | ❌ |
| Auditoría | ✅ | ❌ | ❌ | ❌ |

Si abres una pantalla que tu rol no permite, verás «Acceso denegado». El menú solo muestra lo que puedes usar.

## Administrador

1. **Dashboard:** indicadores y gráficos de toda la operación; filtra por fechas, organización, severidad y categoría.
2. **Incidentes:** crea, edita, asigna a un analista y cambia el estado. Solo se ofrecen los estados permitidos desde el actual (por ejemplo, un incidente «Cerrado» no vuelve a «Abierto»).
3. **Reportes:** elige el tipo, aplica filtros y exporta.
4. **Usuarios:** crea usuarios, cambia el rol o desactiva una cuenta.
5. **Catálogos:** administra severidades, estados, categorías y planes. Un elemento en uso no se puede borrar (verás un aviso).
6. **Auditoría:** consulta quién cambió qué, con los valores anteriores y nuevos, y las exportaciones realizadas.

## Supervisor

Hace lo mismo que el Administrador con incidentes, activos, dashboard y reportes, **sin** administrar usuarios, catálogos ni auditoría, y sin eliminar incidentes ni activos. Su tarea típica es **asignar** los incidentes nuevos al analista con menos carga (la ve en el gráfico «Carga abierta por analista») y vigilar el cumplimiento del SLA.

## Analista

1. En **Incidentes** ves solo los que te asignaron.
2. Abre un incidente y **documenta tu trabajo** en la bitácora; sube las **evidencias** (enlaces web; la API rechaza esquemas peligrosos como `javascript:`).
3. El Analista **no cambia el estado ni edita** el incidente: lo hace el Supervisor o el Administrador.
4. El **dashboard** muestra únicamente tus incidentes.

## Cliente

1. **Mi organización:** datos de tu empresa y de tu contrato SLA.
2. **Incidentes:** solo los de tu organización.
3. **Reportar un incidente:** pulsa **Nuevo incidente**, elige el activo, escribe el título (mínimo 5 caracteres) y la descripción, y pulsa **✨ Sugerir clasificación con IA** si quieres una propuesta de severidad y categoría. **La sugerencia no se aplica sola:** tú decides si la aceptas (**Aplicar sugerencia a severidad**) o eliges otros valores. Si la IA no responde, puedes elegir a mano.
4. **Dashboard y reportes:** muestran solo tu organización.

## Dashboard

1. Elige el período con **Desde** y **Hasta** (por defecto, los últimos 12 meses) y, si tu rol lo permite, organización, severidad y categoría. Pulsa **Aplicar filtros**; **Limpiar** quita los filtros. Si pones «Hasta» antes que «Desde», verás un mensaje de error.
2. Los **6 indicadores** muestran el valor, la variación respecto del período anterior de igual duración y su fórmula.
3. Cada **gráfico** tiene una conclusión escrita y, al abrir **Ver los datos en tabla**, los mismos datos en una tabla.
4. **Resumir con IA** (Administrador y Supervisor) redacta un resumen del período. Lleva la etiqueta «Generado con IA»: revisa el texto antes de usarlo.

## Reportes

1. Elige **Operativo** (detalle de incidentes), **Gerencial** (totales por organización, severidad y estado) o **SLA** (cumplimiento y tiempo de resolución por mes; exige **Desde** y **Hasta**).
2. Aplica los filtros y revisa la tabla y los totales. En el operativo puedes ordenar por columna desde la API (`orden`).
3. **Exportar Excel, PDF o CSV** descarga el archivo con **los mismos filtros** que ves. El CSV usa punto y coma y abre bien en Excel en español. Cada exportación queda registrada en la auditoría. Se permiten hasta 20 por minuto.
4. **Imprimir** abre la vista de impresión: sin menú, filtros ni botones, con el título, la fecha de emisión y los filtros usados.

## Errores frecuentes

| Mensaje | Qué hacer |
|---|---|
| «Correo o contraseña incorrectos, o la cuenta está bloqueada temporalmente» | Revisa los datos; si fueron 5 intentos, espera 15 minutos |
| «Demasiadas solicitudes» | Espera un minuto y vuelve a intentar |
| «Acceso denegado» | Tu rol no incluye esa pantalla; pídele a un administrador si la necesitas |
| «El servicio de IA no está disponible» | Continúa a mano: la IA es opcional |
| «El recurso cambió mientras lo editabas» | Recarga la página y repite el cambio |
