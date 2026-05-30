namespace ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Models;

internal sealed record CoverTemplateSchemaDocument(
    string? SchemaVersion,
    IReadOnlyCollection<CoverTemplateFieldDefinitionModel>? Fields);

internal sealed record CoverTemplateFieldDefinitionModel(
    string? FieldId,
    string? DisplayName,
    string? Placeholder,
    float X,
    float Y,
    float Width,
    float Height,
    string? FontFamily,
    float FontSize,
    string? FontColor,
    bool IsRequired,
    int SortOrder,
    string? DefaultValue,
    string? HorizontalAlignment,
    string? VerticalAlignment,
    int? MaxLength);

internal sealed record CoverTemplateResolvedFieldValueModel(
    CoverTemplateFieldDefinitionModel Definition,
    string Value);