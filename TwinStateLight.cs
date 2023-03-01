namespace SignalRDemo;

internal class TwinStateLight
{
	public required string UnitId { get; set; }

	public int? CurrentFloor { get; set; }

    public string? EquipmentStatus { get; set; }

	public TripDirections? CurrentTripDirection { get; set; }
}
