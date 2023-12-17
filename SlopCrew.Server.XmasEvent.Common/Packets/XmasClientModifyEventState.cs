namespace SlopCrew.Server.XmasEvent;

[Serializable]
public class XmasClientModifyEventStatePacket : XmasPacket {
    public const string PacketId = "Xmas-Client-ModifyEventState";
    public override string GetPacketId() { return XmasClientModifyEventStatePacket.PacketId; }
    protected override uint LatestVersion => 1;

    public List<XmasPhaseModifications> PhaseModifications = [];

    protected override void Write(BinaryWriter writer) {
        writer.Write((UInt16) this.PhaseModifications.Count);
        foreach(var phaseModifications in this.PhaseModifications) {
            phaseModifications.Write(writer);
        }
    }
    protected override void Read(BinaryReader reader) {
        switch(this.Version) {
            case 1:
                this.PhaseModifications = [];
                var phaseCount = reader.ReadUInt16();
                for(var i = 0; i < phaseCount; i++) {
                    var phaseModifications = new XmasPhaseModifications();
                    phaseModifications.Read(reader);
                    this.PhaseModifications.Add(phaseModifications);
                }
                break;
            default:
                this.UnexpectedVersion();
                break;
        }
    }
}

public struct XmasPhaseModifications {
    public XmasPhaseModifications() {}

    // True means this packet should overwrite this value on the server-side

    public bool ModifyActive = false;
    public bool ModifyGiftsCollected = false;
    public bool ModifyGiftsGoal = false;
    public bool ModifyActivatePhaseAutomatically = false;
    public XmasPhase Phase = new();

    public void Write(BinaryWriter writer) {
        writer.Write(this.ModifyActive);
        writer.Write(this.ModifyGiftsCollected);
        writer.Write(this.ModifyGiftsGoal);
        writer.Write(this.ModifyActivatePhaseAutomatically);
        this.Phase.Write(writer);
    }

    public void Read(BinaryReader reader) {
        this.ModifyActive = reader.ReadBoolean();
        this.ModifyGiftsCollected = reader.ReadBoolean();
        this.ModifyGiftsGoal = reader.ReadBoolean();
        this.ModifyActivatePhaseAutomatically = reader.ReadBoolean();
        this.Phase.Read(reader);
    }

}
