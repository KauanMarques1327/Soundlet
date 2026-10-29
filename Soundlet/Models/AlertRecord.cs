namespace Soundlet.Models;

public enum AlertType { Fire, Door, Siren, Doorbell, Baby, Dog }

public sealed record AlertDefinition(string Title, string Description, string Color, string Icon);

public sealed record AlertRecord(Guid Id, AlertType Type, DateTime OccurredAt)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string Title => Definition(Type).Title;
    [System.Text.Json.Serialization.JsonIgnore]
    public string Description => Definition(Type).Description;
    [System.Text.Json.Serialization.JsonIgnore]
    public string Color => Definition(Type).Color;
    [System.Text.Json.Serialization.JsonIgnore]
    public string Icon => Definition(Type).Icon;

    public static AlertDefinition Definition(AlertType type) => type switch
    {
        AlertType.Fire => new("Alerta de incêndio", "Som de alarme de incêndio detectado", "#FF5269", "alert_fire.png"),
        AlertType.Door => new("Batidas na porta", "Possível batida na porta", "#19B9FF", "alert_door.png"),
        AlertType.Siren => new("Alerta de sirene", "Som de sirene detectado", "#FF617B", "alert_siren.png"),
        AlertType.Doorbell => new("Campainha tocando", "Som de campainha detectado", "#FFE45B", "alert_bell.png"),
        AlertType.Baby => new("Choro de bebê", "Som de choro detectado", "#C697FF", "alert_baby.png"),
        _ => new("Latido de cachorro", "Som de latido detectado", "#00D7F4", "alert_dog.png")
    };
}
