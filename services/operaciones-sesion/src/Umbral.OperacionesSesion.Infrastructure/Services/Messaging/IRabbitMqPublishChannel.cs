namespace Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

// Seam mínimo de publicación: el publisher es unit-testeable sin broker;
// la conexión real solo se cubre con el integration test opt-in (B6).
public interface IRabbitMqPublishChannel
{
    void Publish(string routingKey, byte[] body);
}
