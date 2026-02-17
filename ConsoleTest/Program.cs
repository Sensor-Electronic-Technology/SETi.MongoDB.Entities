// See https://aka.ms/new-console-template for more information

using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.Json;
using MongoDB.Driver.Linq.Linq3Implementation;
using ConsoleTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;
using NCalcExtensions;

//Console.WriteLine("Hello, World!");
//RegisterEmbedded<Data>(e => e.EmbeddedList);
//await TestDatabaseMigrations();

var db = await DB.InitAsync(
             "mongo-dev-test",
             new MongoClientSettings() {
                 Server = new("172.20.3.41", 27017),
             });
/*await BuildMigrationRefCollectionProp();
await db.ApplyMigrations();*/
/*await GenerateEpiData();
await DynamicQueryTesting();*/

ValueFieldBuilder builder = new ValueFieldBuilder();
builder.WithDataType(DataType.NUMBER)
       .WithValueInfo(0.00)
       .WithFieldName("Test");

ObjectFieldBuilder objBuilder = new ObjectFieldBuilder();
var field = builder.Build();
objBuilder.WithFieldName("Test")
          .WithTypes(BsonType.Double, TypeCode.Double)
          .WithField(ft => ft.WithFieldName(""));
          /*.AddField<ValueField>(field).AddField(
              new CalculatedFieldBuilder()
                  .SetFieldName("Test2")
                  .SetDataType(DataType.NUMBER)
                  .SetExpression("[power]/([voltage]*[current])]")
                  .AddVariable(new ValueVariableBuilder())
                  .Build());*/

async Task DynamicQueryTesting() {
    /*var db = await DB.InitAsync(
                 "mongo-dev-test",
                 new MongoClientSettings() {
                     Server = new("172.20.3.41", 27017),
                 });*/
    var collection = DB.Default.Collection("mongo-dev-test", "quick_tests").AsQueryable();
    collection = collection.Where(e => e["WaferId"].AsString == "B01-0001-02");

    var query = collection.Select(e => e["InitialMeasurements"]["Power"]);
    var list = query.ToList();
    var output = list.SelectMany(e => e.AsBsonArray).ToList();

    output.ForEach(e => Console.WriteLine(e));
    /*list.ForEach(e=>Console.WriteLine(e));
    var output=list.Select(e=>e.AsBsonArray).ToList();
    output.ForEach(e=>Console.WriteLine(e));*/
    //Console.WriteLine(query.Select(e=>e.ToBsonDocument()).ToString());

    /*var qtCollection=DB.Default.Collection<QuickTest>();
    var result=qtCollection.AsQueryable().SelectMany(e => e.InitialMeasurements.Select(m=>m.Power));*/
    //Console.WriteLine(query.Count());

    /*var collection=DB.Default.Collection("mongo-dev-test","quick_tests").AsQueryable();
    Prop
    var count= collection.Where("e => e.InitialMeasurements.AsBsonArray.Average(m=>m.Power) >= 900").Count();
    Console.WriteLine(count);*/
    //DB.Default.Find<QuickTest>().Match(e => e.InitialMeasurements.Average(e=>e.Power) >= 900);
}

async Task TestDatabaseMigrations() {
    var db = await DB.InitAsync(
                 "mongo-dev-test",
                 new MongoClientSettings() {
                     Server = new("172.20.3.41", 27017),
                 });
    await db.DropCollectionAsync<TypeConfiguration>();
    await db.DropCollectionAsync<DocumentMigration>();
    await db.DropCollectionAsync<EpiRun>();
    await db.DropCollectionAsync<QuickTest>();
    await db.DropCollectionAsync<XrdData>();

    await CreateKeys();
    await GenerateEpiData();
    await BuildMigration();
    await BuildEmbeddedMigration();
    await DB.Default.ApplyMigrations();
}

void TestNCalc() {
    ExtendedExpression expression = new ExtendedExpression("[voltage]*[current]");
    expression.Parameters["voltage"] = 10;
    expression.Parameters["current"] = 20;
    Console.WriteLine(expression.Evaluate());
}

async Task BuildEmbeddedMigration() {
    var migrationNumber = await DB.Default.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    Console.WriteLine("Migration Number: " + migrationNumber);

    MigrationBuilder builder = new MigrationBuilder();
    var calcField = new CalculatedField() {
        FieldName = "WPE",
        BsonType = BsonType.Double,
        TypeCode = TypeCode.Double,
        DataType = DataType.NUMBER,
        DefaultValue = 0.00,
        Expression = "([power]/([volts]*[current]))*100",
        Variables = [
            new PropertyVariable() {
                Property = nameof(QtMeasurement.Power),
                VariableName = "power",
                DataType = DataType.NUMBER
            },
            new PropertyVariable() {
                Property = nameof(QtMeasurement.Current),
                VariableName = "current",
                DataType = DataType.NUMBER
            },
            new PropertyVariable() {
                Property = nameof(QtMeasurement.Voltage),
                VariableName = "voltage",
                DataType = DataType.NUMBER
            }
        ]
    };
    builder.AddField(calcField);
    /*EmbeddedTypeConfiguration? typeConfig =
        await EmbeddedTypeConfiguration.CreateOnline<QuickTest, QtMeasurement>(
            ["InitialMeasurements", "FinalMeasurements"],
            true);*/
    /*await DB.Default.ConfigureType<QuickTest>()
      .RegisterEmbedded(e=>e.FinalMeasurements)
      .RegisterEmbedded(e=>e.InitialMeasurements)
      .RegisterAsync();*/

    var typeConfig = await DB.Default.CreateEmbeddedConfigOnline<QuickTest>(
                         typeof(QtMeasurement),
                         true,
                         ["InitialMeasurements", "FinalMeasurements"]);

    if (typeConfig == null) {
        Console.WriteLine("DocumentTypeConfiguration.CreateOnline failed!");

        return;
    }

    //await typeConfig.SaveAsync();
    await DB.Default.SaveAsync(typeConfig);
    var migration = builder.Build(typeConfig, migrationNumber, nameof(QuickTest) ?? string.Empty);

    //await migration.SaveAsync();
    await DB.Default.SaveAsync(migration);

    //migration.DocumentTypeConfiguration = documentTypeConfig.ToReference();
    await typeConfig.EmbeddedMigrations.AddAsync(migration);

    //await migration.SaveAsync();
    await DB.Default.SaveAsync(migration);
    Console.WriteLine("Migration Created");
}

async Task CreateKeys() {
    await DB.Default.Index<EpiRun>()
            .Key(e => e.WaferId, KeyType.Descending)
            .Option(o => o.Unique = true)
            .CreateAsync();
    await DB.Default.Index<EpiRun>()
            .Key(e => e.RunNumber, KeyType.Descending)
            .Option(o => o.Unique = false)
            .CreateAsync();
    await DB.Default.Index<EpiRun>()
            .Key(e => e.SystemId, KeyType.Descending)
            .Option(o => o.Unique = false)
            .CreateAsync();

    await DB.Default.Index<EpiRun>()
            .Key(e => e.PocketNumber, KeyType.Descending)
            .Option(o => o.Unique = false)
            .CreateAsync();

    await DB.Default.Index<QuickTest>()
            .Key(e => e.WaferId, KeyType.Descending)
            .Option(o => o.Unique = true)
            .CreateAsync();

    await DB.Default.Index<XrdData>()
            .Key(e => e.WaferId, KeyType.Descending)
            .Option(o => o.Unique = true)
            .CreateAsync();

    await DB.Default.Index<Monitoring>()
            .Key(e => e.WaferId, KeyType.Descending)
            .Option(o => o.Unique = true)
            .CreateAsync();
}

async Task BuildMigration() {
    var migrationNumber = await DB.Default.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    Console.WriteLine("Migration Number: " + migrationNumber);

    MigrationBuilder builder = new MigrationBuilder();
    ObjectField objField = new ObjectField {
        FieldName = "Qt Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField {
                FieldName = "Avg. Initial Power",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([powers])",
                Variables = [
                    new CollectionPropertyVariable {
                        Property = "Power",
                        VariableName = "powers",
                        CollectionProperty = "InitialMeasurements",
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                            FilterLogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter> {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    CompareOperator = ComparisonOperator.GreaterThan,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 270,
                                    Filters = new List<Filter> {
                                        new() {
                                            FieldName = "Wavelength",
                                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                                            FilterLogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                        DataType = DataType.LIST_NUMBER
                    }
                ]
            },
            new CalculatedField {
                FieldName = "Avg. Wl",
                BsonType = BsonType.Double,
                DefaultValue = 0.00,
                Expression = "avg([wavelengths])",
                Variables = [
                    new CollectionPropertyVariable {
                        Property = nameof(QtMeasurement.Wavelength),
                        VariableName = "wavelengths",
                        CollectionProperty = nameof(QuickTest.InitialMeasurements),
                        DataType = DataType.LIST_NUMBER,
                        Filter = new() {
                            FieldName = nameof(QtMeasurement.Power),
                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                            FilterLogicalOperator = LogicalOperator.And,
                            Value = 1100,
                            Filters = new List<Filter> {
                                new() {
                                    FieldName = nameof(QtMeasurement.Power),
                                    CompareOperator = ComparisonOperator.GreaterThan,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 500
                                },
                                new() {
                                    FieldName = "Wavelength",
                                    CompareOperator = ComparisonOperator.GreaterThanOrEqual,
                                    FilterLogicalOperator = LogicalOperator.And,
                                    Value = 270,
                                    Filters = new List<Filter> {
                                        new() {
                                            FieldName = "Wavelength",
                                            CompareOperator = ComparisonOperator.LessThanOrEqual,
                                            FilterLogicalOperator = LogicalOperator.Or,
                                            Value = 279
                                        }
                                    }
                                }
                            }
                        },
                    },
                ]
            }
        ]
    };
    builder.AddField(objField);

    //DocumentTypeConfiguration? typeConfig = DocumentTypeConfiguration.CreateOnline<QuickTest>();
    var typeConfig = await DB.Default.CreateDocumentConfigOnline<QuickTest>();

    if (typeConfig == null) {
        Console.WriteLine("DocumentTypeConfiguration.CreateOnly failed!");

        return;
    }

    //await typeConfig.SaveAsync();
    await DB.Default.SaveAsync(typeConfig);
    var migration = builder.Build(typeConfig, migrationNumber);

    //await migration.SaveAsync();
    await DB.Default.SaveAsync(migration);

    //migration.DocumentTypeConfiguration = documentTypeConfig.ToReference();
    await typeConfig.Migrations.AddAsync(migration);

    //await migration.SaveAsync();
    //await migration.SaveAsync();
    await DB.Default.SaveAsync(migration);
    Console.WriteLine("Migration Created");
}

async Task BuildMigrationRefCollectionProp() {
    var migrationNumber = await DB.Default.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    Console.WriteLine("Migration Number: " + migrationNumber);

    MigrationBuilder builder = new MigrationBuilder();
    ObjectField objField = new ObjectField {
        FieldName = "QT Summary",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField {
                FieldName = "InitialAvgPower",
                BsonType = BsonType.Double,
                TypeCode = TypeCode.Double,
                DataType = DataType.NUMBER,
                DefaultValue = 0.00,
                Expression = "avg([powerArr])",
                Variables = [
                    new RefCollectionPropertyVariable() {
                        Property = nameof(QtMeasurement.Power),
                        VariableName = "powerArr",
                        CollectionProperty = nameof(QuickTest.InitialMeasurements),
                        CollectionName = "quick_tests",
                        DatabaseName = "mongo-dev-test",
                        DataType = DataType.LIST_NUMBER,
                        FilterOnEntityId = true,
                        RefEntityIdProperty = nameof(QuickTest.WaferId),
                        EntityIdProperty = nameof(EpiRun.WaferId)
                    }
                ]
            },
            new CalculatedField {
                FieldName = "InitialAvgWl",
                BsonType = BsonType.Double,
                TypeCode = TypeCode.Double,
                DataType = DataType.NUMBER,
                DefaultValue = 0.00,
                Expression = "avg([wlArr])",
                Variables = [
                    new RefCollectionPropertyVariable {
                        Property = nameof(QtMeasurement.Wavelength),
                        VariableName = "wlArr",
                        CollectionProperty = nameof(QuickTest.InitialMeasurements),
                        CollectionName = "quick_tests",
                        DatabaseName = "mongo-dev-test",
                        DataType = DataType.LIST_NUMBER,
                        FilterOnEntityId = true,
                        RefEntityIdProperty = nameof(QuickTest.WaferId),
                        EntityIdProperty = nameof(EpiRun.WaferId)
                    },
                ]
            },
            new CalculatedField {
                FieldName = "FinalMedianPower",
                BsonType = BsonType.Double,
                TypeCode = TypeCode.Double,
                DataType = DataType.NUMBER,
                DefaultValue = 0.00,
                Expression = "median([powerArr])",
                Variables = [
                    new RefCollectionPropertyVariable() {
                        Property = nameof(QtMeasurement.Power),
                        VariableName = "powerArr",
                        CollectionProperty = nameof(QuickTest.FinalMeasurements),
                        CollectionName = "quick_tests",
                        DatabaseName = "mongo-dev-test",
                        DataType = DataType.LIST_NUMBER,
                        FilterOnEntityId = true,
                        RefEntityIdProperty = nameof(QuickTest.WaferId),
                        EntityIdProperty = nameof(EpiRun.WaferId)
                    }
                ]
            },
            new CalculatedField {
                FieldName = "FinalMedianWl",
                BsonType = BsonType.Double,
                TypeCode = TypeCode.Double,
                DataType = DataType.NUMBER,
                DefaultValue = 0.00,
                Expression = "median([wlArr])",
                Variables = [
                    new RefCollectionPropertyVariable {
                        Property = nameof(QtMeasurement.Wavelength),
                        VariableName = "wlArr",
                        CollectionProperty = nameof(QuickTest.FinalMeasurements),
                        CollectionName = "quick_tests",
                        DatabaseName = "mongo-dev-test",
                        DataType = DataType.LIST_NUMBER,
                        FilterOnEntityId = true,
                        RefEntityIdProperty = nameof(QuickTest.WaferId),
                        EntityIdProperty = nameof(EpiRun.WaferId)
                    },
                ]
            }
        ]
    };
    builder.AddField(objField);

    //DocumentTypeConfiguration? typeConfig = DocumentTypeConfiguration.CreateOnline<QuickTest>();
    var typeConfig = await DB.Default.CreateDocumentConfigOnline<EpiRun>();

    if (typeConfig == null) {
        Console.WriteLine("DocumentTypeConfiguration.CreateOnly failed!");

        return;
    }

    //await typeConfig.SaveAsync();
    await DB.Default.SaveAsync(typeConfig);
    var migration = builder.Build(typeConfig, migrationNumber);

    //await migration.SaveAsync();
    await DB.Default.SaveAsync(migration);

    //migration.DocumentTypeConfiguration = documentTypeConfig.ToReference();
    await typeConfig.Migrations.AddAsync(migration);

    //await migration.SaveAsync();
    //await migration.SaveAsync();
    await DB.Default.SaveAsync(migration);
    Console.WriteLine("Migration Created");
}

async Task BuilderMigration3() {
    var migrationNumber = await DB.Default.Collection<DocumentMigration>()
                                  .Find(_ => true)
                                  .SortByDescending(e => e.MigrationNumber)
                                  .Project(e => e.MigrationNumber)
                                  .FirstOrDefaultAsync();
    var collectionName = DB.Default.CollectionName<QuickTest>();
    var typeConfig = await DB.Default.Collection<DocumentTypeConfiguration>()
                             .Find(e => e.CollectionName == collectionName)
                             .FirstOrDefaultAsync();

    if (typeConfig == null) {
        Console.WriteLine("DocumentTypeConfiguration not found");

        return;
    }

    var objField = new ObjectField {
        FieldName = "Qt Pass/Fail",
        BsonType = BsonType.Document,
        TypeCode = TypeCode.Object,
        Fields = [
            new CalculatedField {
                FieldName = "Power Pass/Fail",
                BsonType = BsonType.String,
                IsBooleanExpression = true,
                DefaultValue = "Fail",
                TrueValue = "Pass",
                FalseValue = "Fail",
                Expression = "[pAvg]>[pCriteria]",
                QuantityName = "",
                TypeCode = TypeCode.String,
                Variables = [
                    new ValueVariable {
                        VariableName = "pCriteria",
                        TypeCode = TypeCode.Double,
                        DataType = DataType.NUMBER,
                        Value = 950
                    },
                    new EmbeddedPropertyVariable {
                        VariableName = "pAvg",
                        EmbeddedProperty = "Avg. Initial Power",
                        EmbeddedObjectPropertyPath = ["Qt Summary"],
                        Property = "AdditionalData",
                        DataType = DataType.NUMBER,
                    }
                ]
            },
            new CalculatedField {
                FieldName = "Pass Fail",
                BsonType = BsonType.String,
                IsBooleanExpression = true,
                DefaultValue = "Fail",
                TrueValue = "Pass",
                FalseValue = "Fail",
                Expression = "([pAvg]>[pCriteria]) && ([wlAvg]>=[wlMin] && [wlAvg] <= [wlMax])",
                QuantityName = "",
                TypeCode = TypeCode.String,
                Variables = [
                    new ValueVariable {
                        VariableName = "wlMax",
                        TypeCode = TypeCode.Double,
                        DataType = DataType.NUMBER,
                        Value = 279.5,
                    },
                    new ValueVariable {
                        VariableName = "wlMin",
                        TypeCode = TypeCode.Double,
                        DataType = DataType.NUMBER,
                        Value = 270.5,
                    },
                    new ValueVariable {
                        VariableName = "pCriteria",
                        TypeCode = TypeCode.Double,
                        DataType = DataType.NUMBER,
                        Value = 950
                    },
                    new EmbeddedPropertyVariable {
                        VariableName = "pAvg",
                        EmbeddedProperty = "Avg. Initial Power",
                        EmbeddedObjectPropertyPath = ["Qt Summary"],
                        Property = "AdditionalData",
                        DataType = DataType.NUMBER,
                    },
                    new EmbeddedPropertyVariable {
                        VariableName = "wlAvg",
                        EmbeddedProperty = "Avg. Wl",
                        EmbeddedObjectPropertyPath = ["Qt Summary"],
                        Property = "AdditionalData",
                        DataType = DataType.NUMBER,
                    },
                ]
            }
        ]
    };
    MigrationBuilder builder = new MigrationBuilder();
    builder.AddField(objField);
    var migration = builder.Build(typeConfig, migrationNumber);

    //await migration.SaveAsync();
    Console.WriteLine("Migration saved");
}

async Task GenerateEpiData() {
    var rand = new Random();
    var now = DateTime.Now;
    List<EpiRun> epiRuns = [];
    List<QuickTest> quickTests = [];
    List<XrdData> xrdMeasurementData = [];

    for (int i = 1; i <= 5; i++) {
        for (int x = 1; x <= 10; x++) {
            EpiRun run = new EpiRun {
                RunTypeId = (rand.NextDouble() > .5) ? "Prod" : "Rnd",
                SystemId = "B01",
                TechnicianId = (rand.NextDouble() > .5) ? "RJ" : "NC",
            };
            run.TimeStamp = now;

            string tempId = "";
            string ledId = "";
            string rlId = "";
            GenerateWaferIds(i, "A03", "A02", "B01", ref tempId, ref rlId, ref ledId);

            run.RunNumber = ledId.Substring(ledId.LastIndexOf('-') + 1);

            string tempId_P = tempId;
            string ledId_P = ledId;
            string rlId_P = rlId;

            if (x / 10 >= 1) {
                tempId_P += $"-{x}";
                rlId_P += $"-{x}";
                ledId_P += $"-{x}";
                run.PocketNumber = $"{x}";
            } else {
                tempId_P += $"-0{x}";
                rlId_P += $"-0{x}";
                ledId_P += $"-0{x}";
                run.PocketNumber = $"0{x}";
            }

            run.WaferId = ledId_P;
            epiRuns.Add(run);
            /*await run.SaveAsync();*/

            var quickTestData = new QuickTest {
                WaferId = run.WaferId,

                TimeStamp = now,
                InitialMeasurements = new List<QtMeasurement> {
                    GenerateQtMeasurement(rand, "A", now),
                    GenerateQtMeasurement(rand, "B", now),
                    GenerateQtMeasurement(rand, "C", now),
                    GenerateQtMeasurement(rand, "L", now),
                    GenerateQtMeasurement(rand, "R", now),
                    GenerateQtMeasurement(rand, "T", now),
                    GenerateQtMeasurement(rand, "G", now)
                },
                FinalMeasurements = new List<QtMeasurement> {
                    GenerateQtMeasurement(rand, "A", now),
                    GenerateQtMeasurement(rand, "B", now),
                    GenerateQtMeasurement(rand, "C", now),
                    GenerateQtMeasurement(rand, "L", now),
                    GenerateQtMeasurement(rand, "R", now),
                    GenerateQtMeasurement(rand, "T", now),
                    GenerateQtMeasurement(rand, "G", now)
                }
            };
            quickTests.Add(quickTestData);

            /*await quickTestData.SaveAsync();
            await run.QuickTests.AddAsync(quickTestData);*/
            var xrdData = new XrdData {
                WaferId = run.WaferId,
                XrdMeasurements = new List<XrdMeasurement> {
                    GenerateXrdMeasurement(rand, "C", now),
                    GenerateXrdMeasurement(rand, "Edge", now)
                }
            };
            xrdMeasurementData.Add(xrdData);
            /*await xrdMeasurements.SaveAsync();
            await run.XrdMeasurements.AddAsync(xrdMeasurements);*/
        } //end pocked for loop
    }     //end run number for loop

    await DB.Default.SaveAsync(epiRuns);
    await DB.Default.SaveAsync(quickTests);
    await DB.Default.SaveAsync(xrdMeasurementData);
    /*await epiRuns.SaveAsync();
    await quickTests.SaveAsync();
    await xrdMeasurementData.SaveAsync();*/

    epiRuns.ForEach(run => {
        var qt = quickTests.FirstOrDefault(e => e.WaferId == run.WaferId);
        var xrd = xrdMeasurementData.FirstOrDefault(e => e.WaferId == run.WaferId);

        if (qt != null) {
            run.QuickTest = qt.ToReference();
            qt.EpiRun = run.ToReference();
        }

        if (xrd != null) {
            run.XrdData = xrd.ToReference();
            xrd.EpiRun = run.ToReference();
        }
    });

    /*await epiRuns.SaveAsync();
    await quickTests.SaveAsync();
    await xrdMeasurementData.SaveAsync();*/

    await DB.Default.SaveAsync(epiRuns);
    await DB.Default.SaveAsync(quickTests);
    await DB.Default.SaveAsync(xrdMeasurementData);
    Console.WriteLine("Check Database");
}

XrdMeasurement GenerateXrdMeasurement(Random rand, string Area, DateTime now) {
    var xrd = new XrdMeasurement {
        XrdArea = Area,
        TimeStamp = now,
        Alpha_AlN = NextDouble(rand, 35.937, 36.0211),
        Beta_AlN = NextDouble(rand, 35.9472, 36.0754),
        FHWM102 = NextDouble(rand, 180, 568.8),
        FWHM002 = NextDouble(rand, 7.2, 358.8),
        dOmega = NextDouble(rand, 0.0065, .3748),
        Omega = NextDouble(rand, 16.2183, 18.3815)
    };

    return xrd;
}

QtMeasurement GenerateQtMeasurement(Random rand, string Area, DateTime now) {
    var qt = new QtMeasurement {
        Area = Area,
        TimeStamp = now,
        Current = 20.0,
        Power = NextDouble(rand, 700, 1700),
        Voltage = NextDouble(rand, 9.5, 15.5),
        Wavelength = NextDouble(rand, 270, 279.9)
    };

    return qt;
}

void GenerateWaferIds(int i,
                      string tempSystem,
                      string rlSystem,
                      string ledSystem,
                      ref string tempId,
                      ref string rlId,
                      ref string ledId) {
    tempId = tempSystem;
    rlId = rlSystem;
    ledId = ledSystem;

    if (i / 1000 >= 1) {
        tempId += $"-{i}";
        rlId += $"-{i}";
        ledId += $"-{i}";
    } else if (i / 100 >= 1) {
        tempId += $"-0{i}";
        rlId += $"-0{i}";
        ledId += $"-0{i}";
    } else if (i / 10 >= 1) {
        tempId += $"-00{i}";
        rlId += $"-00{i}";
        ledId += $"-00{i}";
    } else {
        tempId += $"-000{i}";
        rlId += $"-000{i}";
        ledId += $"-000{i}";
    }
}

double NextDouble(Random rand, double min, double max)
    => rand.NextDouble() * (max - min) + min;

public class Data : DocumentEntity {
    public string Name { get; set; }
    public int Age { get; set; }
    public string Address { get; set; }
    public EmbeddedOtherData? Other { get; set; }
    public EmbeddedData? Embedded { get; set; }
}

public class DataEmbeddedList : DocumentEntity {
    public string Name { get; set; }
    public int Age { get; set; }
    public string Address { get; set; }
    public List<EmbeddedData> Embedded { get; set; } = [];
}

public class DataOther : DocumentEntity {
    public string Name { get; set; }
    public int Age { get; set; }
    public string Address { get; set; }
    public EmbeddedOtherData? Other { get; set; }
}

public class EmbeddedData : IEmbeddedEntity {
    public double Value { get; set; }
    public BsonDocument? AdditionalData { get; set; }
}

public class EmbeddedOtherData : IEmbeddedEntity {
    public double Slope { get; set; }
    public double Intercept { get; set; }
    public BsonDocument? AdditionalData { get; set; }
}

/*void RegisterEmbedded<TEntity>(Expression<Func<TEntity, object?>>? embeddedProperty = null) where TEntity : IEntity {
    if (embeddedProperty is null) {
        Console.WriteLine("No embedded property specified!");

        return;
    }
    string path = Prop.Path(embeddedProperty);

    Console.WriteLine(typeof(TEntity).GetProperty(path)?.PropertyType);

    /*if (path.Contains('.')) {
        var props = path.Split('.');
        Console.WriteLine(typeof(TEntity).GetProperty(props[0])?.PropertyType.IsAssignableTo(typeof(IEmbeddedEntity)));
        Console.WriteLine(typeof(TEntity).GetProperty(props[0])?.PropertyType.GetProperty(props[1])?.PropertyType.IsAssignableTo(typeof(IEmbeddedEntity)));
    }#1#
    var type = typeof(TEntity).GetProperty(path)?.PropertyType;

    if (type != typeof(string) && (type.IsArray || type.IsAssignableTo(typeof(IEnumerable)))) {
        Console.WriteLine(
            $"IsArray and IsAssignable: {type.GetElementType()?.IsAssignableTo(typeof(IEmbeddedEntity))}");
    } else {
        Console.WriteLine($"NotArray and IsAssignable: {type?.IsAssignableTo(typeof(IEmbeddedEntity)) ?? false}");
    }

    /*Console.WriteLine(Prop.Path(embeddedProperty));
    Console.WriteLine(Prop.Property(embeddedProperty));#1#
}*/

/*public static class Prop {
    static readonly Regex _rxOne =
        new(
            @"(?:\.(?:\w+(?:[[(]\d+[)\]])?))+",
            RegexOptions.Compiled); //matched result: One.Two[1].Three.get_Item(2).Four

    static readonly Regex _rxTwo =
        new(@".get_Item\((\d+)\)", RegexOptions.Compiled); //replaced result: One.Two[1].Three[2].Four

    static readonly Regex _rxThree = new(@"\[\d+\]", RegexOptions.Compiled);
    static readonly Regex _rxFour = new(@"\[(\d+)\]", RegexOptions.Compiled);

    static string ToLowerCaseLetter(long n) {
        if (n < 0)
            throw new NotSupportedException("Value must be greater than 0!");

        string? val = null;
        const char c = 'a';

        while (n >= 0) {
            val = (char)(c + n % 26) + val;
            n /= 26;
            n--;
        }

        return val!;
    }

    static void ThrowIfInvalid<T>(Expression<Func<T, object?>> expression) {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression), "The supplied expression is null!");

        if (expression.Body.NodeType == ExpressionType.Parameter)
            throw new ArgumentException("Cannot generate property path from lambda parameter!");
    }

    static string GetPath<T>(Expression<Func<T, object?>> expression) {
        ThrowIfInvalid(expression);

        return _rxTwo.Replace(
            _rxOne.Match(expression.ToString()).Value[1..],
            m => "[" + m.Groups[1].Value + "]");
    }

    internal static string GetPath(string expString) {
        return
            _rxThree.Replace(
                _rxTwo.Replace(
                    _rxOne.Match(expString).Value[1..],
                    m => "[" + m.Groups[1].Value + "]"),
                "");
    }

    /// <summary>
    /// Returns the name of the property for a given expression.
    /// <para>EX: Authors[0].Books[0].Title > Title</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string Property<T>(Expression<Func<T, object?>> expression) {
        ThrowIfInvalid(expression);

        return expression.MemberInfo().Name;
    }

    /// <summary>
    /// Returns the full dotted path for a given expression.
    /// <para>EX: Authors[0].Books[0].Title > Authors.Books.Title</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string Path<T>(Expression<Func<T, object?>> expression)
        => _rxThree.Replace(GetPath(expression), "");

    /// <summary>
    /// Returns a path with filtered positional identifiers $[x] for a given expression.
    /// <para>EX: Authors[0].Name > Authors.$[a].Name</para>
    /// <para>EX: Authors[1].Age > Authors.$[b].Age</para>
    /// <para>EX: Authors[2].Books[3].Title > Authors.$[c].Books.$[d].Title</para>
    /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string PosFiltered<T>(Expression<Func<T, object?>> expression) {
        return _rxFour.Replace(
            GetPath(expression),
            m => ".$[" + ToLowerCaseLetter(int.Parse(m.Groups[1].Value)) + "]");
    }

    /// <summary>
    /// Returns a path with the all positional operator $[] for a given expression.
    /// <para>EX: Authors[0].Name > Authors.$[].Name</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string PosAll<T>(Expression<Func<T, object?>> expression)
        => _rxThree.Replace(GetPath(expression), ".$[]");

    /// <summary>
    /// Returns a path with the first positional operator $ for a given expression.
    /// <para>EX: Authors[0].Name > Authors.$.Name</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string PosFirst<T>(Expression<Func<T, object?>> expression)
        => _rxThree.Replace(GetPath(expression), ".$");

    /// <summary>
    /// Returns a path without any filtered positional identifier prepended to it.
    /// <para>EX: b => b.Tags > Tags</para>
    /// </summary>
    /// <param name="expression">x => x.SomeProp</param>
    public static string Elements<T>(Expression<Func<T, object?>> expression)
        => Path(expression);

    /// <summary>
    /// Returns a path with the filtered positional identifier prepended to the property path.
    /// <para>EX: 0, x => x.Rating > a.Rating</para>
    /// <para>EX: 1, x => x.Rating > b.Rating</para>
    /// <para>TIP: Index positions start from '0' which is converted to 'a' and so on.</para>
    /// </summary>
    /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
    /// <param name="expression">x => x.SomeProp</param>
    public static string Elements<T>(int index, Expression<Func<T, object?>> expression)
        => $"{ToLowerCaseLetter(index)}.{Path(expression)}";
}

public static partial class Extensions {
    internal static PropertyInfo PropertyInfo<T>(this Expression<T> expression)
        => (PropertyInfo)expression.MemberInfo();

    internal static MemberInfo MemberInfo<T>(this Expression<T> expression)
        => expression.Body switch {
            MemberExpression m => m.Member,
            UnaryExpression { Operand: MemberExpression m } => m.Member,
            _ => throw new NotSupportedException($"[{expression}] is not a valid member expression!")
        };
}*/