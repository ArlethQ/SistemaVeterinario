# Facturación Numérica integrada

Este módulo contiene localmente el código que el sistema veterinario necesita de Numérica.
El proyecto principal ya no depende del repositorio externo `E-FACT`.

## Alcance habilitado

- Emisión y consulta de facturas, notas de crédito, notas de débito y guías de remisión.
- Liquidaciones de compra, retenciones e importación de compras XML.
- Emisor, certificado/firma tributaria, cajas, series y secuenciales.
- Catálogos necesarios para facturar: clientes, productos, impuestos, formas de pago y tipos de identificación.
- Contabilidad EContax: compras, cartera, cuentas, inventario, parametrización, reportes y seguridad requerida.

## Fuera de alcance

No se incluyen en el sistema veterinario las pantallas, controladores ni carpetas de EDeclara,
ESign, Aliados, BackOffice, recargas, suscripciones o solicitudes comerciales. El cálculo de
comisiones requerido por las facturas quedó aislado bajo `Services/Facturacion` y
`Models/Facturacion`.

Las vistas visibles de facturación están físicamente en `Components/Pages` del proyecto
`HosvetKardex.Web`; ya no son páginas adaptadoras que renderizan un componente externo.
