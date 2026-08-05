using System.Net;
using System.Text.Json;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;

namespace HandballIntegration.Admin.Services;

public sealed class AdminProblemDetailsMapper : IProblemDetailsMapper
{
    public AdminClientError Map(HttpStatusCode status, string? responseBody)
    {
        string? code = null;
        string? correlationId = null;
        bool retryable = false;
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;
                code = ReadString(root, "code");
                correlationId = ReadString(root, "correlationId");
                retryable = root.TryGetProperty("retryable", out var retryableNode) &&
                            retryableNode.ValueKind == JsonValueKind.True;
            }
            catch (JsonException)
            {
                // Raw payloads are intentionally ignored.
            }
        }

        code ??= status switch
        {
            HttpStatusCode.Unauthorized => "ADMIN_UNAUTHORIZED",
            HttpStatusCode.Forbidden => "ADMIN_FORBIDDEN",
            HttpStatusCode.NotFound => "ADMIN_NOT_FOUND",
            HttpStatusCode.Conflict => "ADMIN_CONFLICT",
            HttpStatusCode.PreconditionFailed => "ADMIN_PRECONDITION_FAILED",
            _ when (int)status == 428 => "ADMIN_PRECONDITION_REQUIRED",
            _ => "ADMIN_OPERATION_FAILED"
        };

        var (message, action) = code switch
        {
            "ADMIN_PRECONDITION_FAILED" => (
                "Cette donnee a ete modifiee par un autre utilisateur.",
                "Actualisez puis reappliquez manuellement vos changements."),
            "ADMIN_DEPENDENCY_CONFLICT" => (
                "Cette operation est bloquee par des dependances.",
                "Consultez l'analyse d'impact et traitez les dependances indiquees."),
            "ADMIN_FORBIDDEN" => (
                "Vous ne disposez pas de l'autorisation requise.",
                "Contactez un administrateur si cet acces est necessaire."),
            "ADMIN_UNAUTHORIZED" => (
                "Votre session n'est plus valide.",
                "Reconnectez-vous pour continuer."),
            "ADMIN_IMPORT_PREVIEW_EXPIRED" => (
                "L'apercu d'import a expire.",
                "Generez un nouvel apercu avant d'executer l'import."),
            _ => (
                "L'operation n'a pas pu etre terminee.",
                retryable ? "Reessayez dans quelques instants." : "Verifiez les donnees puis reessayez.")
        };

        return new AdminClientError(status, code, message, action, retryable, correlationId);
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
