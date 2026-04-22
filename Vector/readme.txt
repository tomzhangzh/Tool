SalesDataAnalyzer/
├── SalesDataAnalyzer.Models/          # 数据实体模型
│   ├── SalesCategory.cs              # 销售类别数据
│   ├── HourlySales.cs                # 小时销售数据
│   ├── SalesSummary.cs               # 销售汇总数据
│   ├── PaymentMethod.cs              # 支付方式数据
│   └── VectorData.cs                 # 向量数据记录
├── SalesDataAnalyzer.Data/           # 数据访问层
│   ├── SalesDbContext.cs             # EF Core数据库上下文
│   └── Repositories/
│       ├── ISalesRepository.cs       # 仓储接口
│       └── SalesRepository.cs        # 仓储实现
├── SalesDataAnalyzer.Services/       # 业务逻辑层
│   ├── XmlParsers/                   # XML解析器
│   ├── VectorDb/                     # Chroma向量数据库集成
│   │   ├── IChromaService.cs
│   │   └── ChromaService.cs
│   └── DataImportService.cs          # 数据导入和分析服务
└── SalesDataAnalyzer.Console/        # 控制台应用入口
    └── Program.cs


    & 'C:\Users\Tom\AppData\Local\Packages\PythonSoftwareFoundation.Python.3.11_qbz5n2kfra8p0\LocalCache\local-packages\Python311\Scripts\chroma.exe' run --host localhost --port 8000
