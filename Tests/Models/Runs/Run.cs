namespace MongoDB.Entities.Tests.Models.Runs;

public class Run:Entity {
    public string RunNumber { get; set; }
    public Many<RunItem,Run> Items { get; set; }
    
    [InverseSide]
    public Many<RunCategory,Run> Categories { get; set; }
    
    public Run() {
        this.InitOneToMany(() => Items);
        this.InitManyToMany(()=>Categories, c=>c.Runs);
    }
}

public class RunItem:Entity {
    public int ItemNumber { get; set; }
    public One<Run>? Run { get; set; }
}

public class RunCategory:Entity {
    public string Name { get; set; }
    [OwnerSide]
    public Many<Run,RunCategory> Runs { get; set; }

    public RunCategory() {
        this.InitManyToMany(()=>Runs, r=>r.Categories);
    }
}
