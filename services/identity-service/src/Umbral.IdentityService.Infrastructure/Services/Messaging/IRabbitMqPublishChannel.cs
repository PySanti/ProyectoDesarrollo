namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

// Seam mínimo de publicación: el publisher es unit-testeable sin broker;
// la conexión real solo se cubre con el integration test opt-in.
public interface IRabbitMqPublishChannel
{
    void Publish(string routingKey, byte[] body);
}
