namespace TiendaApi.Api.Services.Email;

/// <summary>
/// Plantillas base para emails de la tienda.
/// </summary>
public static class EmailTemplates
{
    /// <summary>
    /// Crea el HTML base para un email de la tienda.
    /// </summary>
    public static string CreateBase(string title, string content)
    {
        return $@"<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
</head>
<body style='font-family: 'Segoe UI', Arial, sans-serif; background-color: #f0f2f5; margin: 0; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center;'>
            <h1 style='margin: 0; color: #ffffff; font-size: 28px; font-weight: 600;'>🛒 Tienda DAW</h1>
            <p style='margin: 8px 0 0 0; color: rgba(255,255,255,0.9); font-size: 14px;'>Tu tienda online de confianza</p>
        </div>
        
        <!-- Content -->
        <div style='padding: 30px;'>
            <h2 style='color: #1a1a2e; margin-top: 0; font-size: 22px; border-bottom: 2px solid #667eea; padding-bottom: 10px;'>{title}</h2>
            <div style='color: #4a4a68; line-height: 1.8; font-size: 15px;'>
                {content}
            </div>
        </div>
        
        <!-- Footer -->
        <div style='background-color: #f8f9fa; padding: 20px; text-align: center; border-top: 1px solid #e9ecef;'>
            <p style='margin: 0; color: #6c757d; font-size: 12px;'>© 2026 Tienda DAW. Todos los derechos reservados.</p>
            <p style='margin: 5px 0 0 0; color: #6c757d; font-size: 12px;'>
                ¿Tienes preguntas? Escríbenos a <a href='mailto:soporte@tiendadaw.com' style='color: #667eea;'>soporte@tiendadaw.com</a>
            </p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Genera el contenido para email de nuevo producto.
    /// </summary>
    public static string ProductoCreado(string nombre, decimal precio, int stock, long id)
    {
        return $@"
            <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                <tr>
                    <td style='padding: 12px; background-color: #f8f9fa; font-weight: 600; width: 120px;'>ID:</td>
                    <td style='padding: 12px;'>{id}</td>
                </tr>
                <tr>
                    <td style='padding: 12px; background-color: #f8f9fa; font-weight: 600;'>Nombre:</td>
                    <td style='padding: 12px;'>{nombre}</td>
                </tr>
                <tr>
                    <td style='padding: 12px; background-color: #f8f9fa; font-weight: 600;'>Precio:</td>
                    <td style='padding: 12px; color: #28a745; font-weight: 600;'>{precio:N2}€</td>
                </tr>
                <tr>
                    <td style='padding: 12px; background-color: #f8f9fa; font-weight: 600;'>Stock:</td>
                    <td style='padding: 12px;'>{stock} unidades</td>
                </tr>
            </table>
            <p style='margin-top: 20px; padding: 15px; background-color: #e7f3ff; border-left: 4px solid #667eea; border-radius: 4px;'>
                ✅ El producto ya está disponible en la tienda.
            </p>";
    }

    /// <summary>
    /// Genera el contenido para email de nuevo pedido (incluye datos del cliente).
    /// </summary>
    public static string PedidoCreado(string pedidoId, decimal total, int itemCount, long userId)
    {
        return $@"
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                <h3 style='margin-top: 0; color: #1a1a2e;'>📋 Datos del Cliente</h3>
                <table style='width: 100%; border-collapse: collapse; margin-bottom: 15px;'>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>ID Cliente:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{userId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Número de pedido:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600; color: #1a1a2e;'>#{pedidoId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Total:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600; color: #28a745; font-size: 18px;'>{total:N2}€</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Artículos:</td>
                        <td style='padding: 8px 0; text-align: right;'>{itemCount} productos</td>
                    </tr>
                </table>
            </div>
            <p style='padding: 15px; background-color: #d4edda; border-left: 4px solid #28a745; border-radius: 4px; color: #155724;'>
                ✅ Tu pedido ha sido recibido y está siendo procesado.
            </p>";
    }

    /// <summary>
    /// Genera el contenido para email de cambio de estado de pedido (incluye datos del cliente).
    /// </summary>
    public static string PedidoEstadoActualizado(string pedidoId, string estadoAnterior, string nuevoEstado, decimal total, long userId)
    {
        var estadoColor = nuevoEstado.ToUpper() switch
        {
            "ENTREGADO" => "#28a745",
            "ENVIADO" => "#17a2b8",
            "PROCESANDO" => "#ffc107",
            "CANCELADO" => "#dc3545",
            _ => "#6c757d"
        };

        return $@"
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                <h3 style='margin-top: 0; color: #1a1a2e;'>📋 Datos del Cliente</h3>
                <table style='width: 100%; border-collapse: collapse; margin-bottom: 15px;'>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>ID Cliente:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{userId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Pedido:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>#{pedidoId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Estado anterior:</td>
                        <td style='padding: 8px 0; text-align: right; text-decoration: line-through; color: #6c757d;'>{estadoAnterior}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Estado nuevo:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600; color: {estadoColor};'>{nuevoEstado}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Total:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{total:N2}€</td>
                    </tr>
                </table>
            </div>
            <p style='padding: 15px; background-color: #e7f3ff; border-left: 4px solid #667eea; border-radius: 4px;'>
                📦 Te mantendremos informado sobre el estado de tu envío.
            </p>";
    }

    /// <summary>
    /// Genera el contenido para email de pedido actualizado por administrador (incluye datos del cliente).
    /// </summary>
    public static string PedidoActualizadoAdmin(string pedidoId, string estado, decimal total, long userId)
    {
        var estadoColor = estado.ToUpper() switch
        {
            "ENTREGADO" => "#28a745",
            "ENVIADO" => "#17a2b8",
            "PROCESANDO" => "#ffc107",
            "CANCELADO" => "#dc3545",
            _ => "#6c757d"
        };

        return $@"
            <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #ffc107;'>
                <h3 style='margin-top: 0; color: #856404;'>⚠️ Actualización por Administrador</h3>
            </div>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                <h3 style='margin-top: 0; color: #1a1a2e;'>📋 Datos del Cliente</h3>
                <table style='width: 100%; border-collapse: collapse; margin-bottom: 15px;'>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>ID Cliente:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{userId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Pedido:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>#{pedidoId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Estado actual:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600; color: {estadoColor};'>{estado}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Total:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{total:N2}€</td>
                    </tr>
                </table>
            </div>
            <p style='padding: 15px; background-color: #e7f3ff; border-left: 4px solid #667eea; border-radius: 4px;'>
                ℹ️ Este pedido ha sido modificado por un administrador del sistema.
            </p>";
    }

    /// <summary>
    /// Genera el contenido para email de pedido eliminado por administrador (incluye datos del cliente).
    /// </summary>
    public static string PedidoEliminadoAdmin(string pedidoId, decimal total, long userId)
    {
        return $@"
            <div style='background-color: #f8d7da; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #f5c6cb;'>
                <h3 style='margin-top: 0; color: #721c24;'>🗑️ Pedido Eliminado</h3>
            </div>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                <h3 style='margin-top: 0; color: #1a1a2e;'>📋 Datos del Cliente</h3>
                <table style='width: 100%; border-collapse: collapse; margin-bottom: 15px;'>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>ID Cliente:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{userId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Pedido eliminado:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>#{pedidoId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6c757d;'>Total del pedido:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: 600;'>{total:N2}€</td>
                    </tr>
                </table>
            </div>
            <p style='padding: 15px; background-color: #f8d7da; border-left: 4px solid #dc3545; border-radius: 4px; color: #721c24;'>
                ⚠️ Este pedido ha sido eliminado del sistema. El stock de los productos ha sido restaurado.
            </p>";
    }
}
