using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TUI.Services.DBModel
{
    public partial class TUIDbContext : DbContext
    {
        public TUIDbContext()
        {
        }

        public TUIDbContext(DbContextOptions<TUIDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ApiLog> ApiLog { get; set; }
        public virtual DbSet<Attachment> Attachment { get; set; }
        public virtual DbSet<AuditData> AuditData { get; set; }
        public virtual DbSet<DETAILITEM> DETAILITEM { get; set; }
        public virtual DbSet<DETAILPAGE> DETAILPAGE { get; set; }
        public virtual DbSet<DICTSETTING> DICTSETTING { get; set; }
        public virtual DbSet<EmailTemplate> EmailTemplate { get; set; }
        public virtual DbSet<FunctionObject> FunctionObject { get; set; }
        public virtual DbSet<GRIDITEM> GRIDITEM { get; set; }
        public virtual DbSet<GRIDPAGE> GRIDPAGE { get; set; }
        public virtual DbSet<LogEvents> LogEvents { get; set; }
        public virtual DbSet<PriceBrand> PriceBrand { get; set; }
        public virtual DbSet<PriceBrandConfiguration> PriceBrandConfiguration { get; set; }
        public virtual DbSet<PriceBrandConfigurationHistory> PriceBrandConfigurationHistory { get; set; }
        public virtual DbSet<PriceBrandFormula> PriceBrandFormula { get; set; }
        public virtual DbSet<PriceEmailRecipient> PriceEmailRecipient { get; set; }
        public virtual DbSet<PriceFaxRecipient> PriceFaxRecipient { get; set; }
        public virtual DbSet<PriceSmsRecipient> PriceSmsRecipient { get; set; }
        public virtual DbSet<PriceTask> PriceTask { get; set; }
        public virtual DbSet<Role> Role { get; set; }
        public virtual DbSet<RolePermission> RolePermission { get; set; }
        public virtual DbSet<ScheduleTask> ScheduleTask { get; set; }
        public virtual DbSet<ScheduleTaskLog> ScheduleTaskLog { get; set; }
        public virtual DbSet<SendLog> SendLog { get; set; }
        public virtual DbSet<SystemSettingItem> SystemSettingItem { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<UsersInRole> UsersInRole { get; set; }
        public virtual DbSet<tblAccount> tblAccount { get; set; }
        public virtual DbSet<tblAccountAccessList> tblAccountAccessList { get; set; }
        public virtual DbSet<tblAccountLoginHistory> tblAccountLoginHistory { get; set; }
        public virtual DbSet<tblAccountResetPassword> tblAccountResetPassword { get; set; }
        public virtual DbSet<tblAwsSns> tblAwsSns { get; set; }
        public virtual DbSet<tblBouncedEmail> tblBouncedEmail { get; set; }
        public virtual DbSet<tblCarrierDeliveryLocations> tblCarrierDeliveryLocations { get; set; }
        public virtual DbSet<tblCarrierGulfstream> tblCarrierGulfstream { get; set; }
        public virtual DbSet<tblCarrierGulfstreamLog> tblCarrierGulfstreamLog { get; set; }
        public virtual DbSet<tblCarrierInvoiceDBTrucking> tblCarrierInvoiceDBTrucking { get; set; }
        public virtual DbSet<tblCarrierInvoiceDBTruckingLog> tblCarrierInvoiceDBTruckingLog { get; set; }
        public virtual DbSet<tblCarrierInvoiceESP> tblCarrierInvoiceESP { get; set; }
        public virtual DbSet<tblCarrierInvoiceESPLog> tblCarrierInvoiceESPLog { get; set; }
        public virtual DbSet<tblCarrierInvoiceProEnergy> tblCarrierInvoiceProEnergy { get; set; }
        public virtual DbSet<tblCarrierInvoiceProEnergyLog> tblCarrierInvoiceProEnergyLog { get; set; }
        public virtual DbSet<tblCarrierLoadDBTrucking> tblCarrierLoadDBTrucking { get; set; }
        public virtual DbSet<tblCarrierLoadDBTruckingLog> tblCarrierLoadDBTruckingLog { get; set; }
        public virtual DbSet<tblCompany> tblCompany { get; set; }
        public virtual DbSet<tblConfiguration> tblConfiguration { get; set; }
        public virtual DbSet<tblDistributorTaxExclusion> tblDistributorTaxExclusion { get; set; }
        public virtual DbSet<tblESPTest> tblESPTest { get; set; }
        public virtual DbSet<tblEmailLog> tblEmailLog { get; set; }
        public virtual DbSet<tblEmailTemplate> tblEmailTemplate { get; set; }
        public virtual DbSet<tblFaxLog> tblFaxLog { get; set; }
        public virtual DbSet<tblGasStation> tblGasStation { get; set; }
        public virtual DbSet<tblGasStationAddress> tblGasStationAddress { get; set; }
        public virtual DbSet<tblGasType> tblGasType { get; set; }
        public virtual DbSet<tblGoogleDirectionsServiceLog> tblGoogleDirectionsServiceLog { get; set; }
        public virtual DbSet<tblOrder> tblOrder { get; set; }
        public virtual DbSet<tblOrderFuelDetails> tblOrderFuelDetails { get; set; }
        public virtual DbSet<tblPickupCustomer> tblPickupCustomer { get; set; }
        public virtual DbSet<tblPortalAccount> tblPortalAccount { get; set; }
        public virtual DbSet<tblPortalApiLog> tblPortalApiLog { get; set; }
        public virtual DbSet<tblPortalLoginStats> tblPortalLoginStats { get; set; }
        public virtual DbSet<tblPortalResetPassword> tblPortalResetPassword { get; set; }
        public virtual DbSet<tblPriceBrand> tblPriceBrand { get; set; }
        public virtual DbSet<tblPriceBrandConfiguration> tblPriceBrandConfiguration { get; set; }
        public virtual DbSet<tblPriceBrandNew> tblPriceBrandNew { get; set; }
        public virtual DbSet<tblPriceEmailRecipient> tblPriceEmailRecipient { get; set; }
        public virtual DbSet<tblPriceEmailRecipientLog> tblPriceEmailRecipientLog { get; set; }
        public virtual DbSet<tblPriceFaxRecipient> tblPriceFaxRecipient { get; set; }
        public virtual DbSet<tblPriceHistory> tblPriceHistory { get; set; }
        public virtual DbSet<tblPriceNotificationEmail> tblPriceNotificationEmail { get; set; }
        public virtual DbSet<tblPriceNotificationMapping> tblPriceNotificationMapping { get; set; }
        public virtual DbSet<tblPriceNotificationType> tblPriceNotificationType { get; set; }
        public virtual DbSet<tblPriceProduct> tblPriceProduct { get; set; }
        public virtual DbSet<tblPriceSentHistory> tblPriceSentHistory { get; set; }
        public virtual DbSet<tblPriceSmsRecipient> tblPriceSmsRecipient { get; set; }
        public virtual DbSet<tblPriceTask> tblPriceTask { get; set; }
        public virtual DbSet<tblSendGridHookLog> tblSendGridHookLog { get; set; }
        public virtual DbSet<tblSmsLog> tblSmsLog { get; set; }
        public virtual DbSet<tblSmsTemplate> tblSmsTemplate { get; set; }
        public virtual DbSet<tbl_upload> tbl_upload { get; set; }
        public virtual DbSet<tbl_utest> tbl_utest { get; set; }
        public virtual DbSet<vwCarrierInvoiceSummary> vwCarrierInvoiceSummary { get; set; }
        public virtual DbSet<vwCarrierUniqueDeliveryLocations> vwCarrierUniqueDeliveryLocations { get; set; }
        public virtual DbSet<vwDBTruckingDetail> vwDBTruckingDetail { get; set; }
        public virtual DbSet<vwDeliveryDetails> vwDeliveryDetails { get; set; }
        public virtual DbSet<vwESPDetail> vwESPDetail { get; set; }
        public virtual DbSet<vwGulfstreamDetail> vwGulfstreamDetail { get; set; }
        public virtual DbSet<vwProEnergyDetail> vwProEnergyDetail { get; set; }
        public virtual DbSet<vw_tblESPTest_BaseView> vw_tblESPTest_BaseView { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=.;Database=TUI;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApiLog>(entity =>
            {
                entity.Property(e => e.ApiType).IsRequired();

                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.Property(e => e.StartDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.Property(e => e.CreateDateTime).HasColumnType("datetime");

                entity.Property(e => e.FileFormat).HasMaxLength(50);

                entity.Property(e => e.FileName).HasMaxLength(255);

                entity.Property(e => e.FileType).HasMaxLength(50);

                entity.Property(e => e.ObjType).HasMaxLength(50);

                entity.Property(e => e.TempIdForNew).HasMaxLength(50);
            });

            modelBuilder.Entity<AuditData>(entity =>
            {
                entity.Property(e => e.DBName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ExecuteTime).HasColumnType("datetime");

                entity.Property(e => e.ExecuteTimeUtc).HasColumnType("datetime");

                entity.Property(e => e.Keys).IsRequired();

                entity.Property(e => e.LoginName).HasMaxLength(50);

                entity.Property(e => e.Server).HasMaxLength(255);

                entity.Property(e => e.TableName)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<DETAILITEM>(entity =>
            {
                entity.Property(e => e.COLUMNNAME)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.COLUMNTYPE).HasMaxLength(50);

                entity.Property(e => e.ISSHOW)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.LABEL).HasMaxLength(50);

                entity.Property(e => e.VALIDATETYPE).HasMaxLength(50);

                entity.HasOne(d => d.DETAILPAGE)
                    .WithMany(p => p.DETAILITEM)
                    .HasForeignKey(d => d.DETAILPAGEID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DETAILITEM_DETAILITEM");
            });

            modelBuilder.Entity<DETAILPAGE>(entity =>
            {
                entity.Property(e => e.CLASSNAME).HasMaxLength(255);

                entity.Property(e => e.NAME)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<DICTSETTING>(entity =>
            {
                entity.Property(e => e.NAME).HasMaxLength(50);

                entity.Property(e => e.TABLENAME).HasMaxLength(50);

                entity.Property(e => e.TYPE).HasMaxLength(50);

                entity.Property(e => e.VALUE).HasMaxLength(50);
            });

            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.Property(e => e.Category).IsRequired();

                entity.Property(e => e.TemplateName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(d => d.BodyFooterTemplate)
                    .WithMany(p => p.InverseBodyFooterTemplate)
                    .HasForeignKey(d => d.BodyFooterTemplateID)
                    .HasConstraintName("FK_EmailTemplate_EmailTemplate");
            });

            modelBuilder.Entity<FunctionObject>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(255);

                entity.Property(e => e.FunctionObjectName).HasMaxLength(255);

                entity.Property(e => e.FunctionObjectName_En).HasMaxLength(255);

                entity.Property(e => e.PermissonTag)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasOne(d => d.ParentFunctionObject)
                    .WithMany(p => p.InverseParentFunctionObject)
                    .HasForeignKey(d => d.ParentFunctionObjectId)
                    .HasConstraintName("FK_FunctionObject_FunctionObject");
            });

            modelBuilder.Entity<GRIDITEM>(entity =>
            {
                entity.Property(e => e.COLUMNNAME)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FORMAT).HasMaxLength(50);

                entity.Property(e => e.ISSHOW)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.LABEL)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.SORTNAME).HasMaxLength(50);

                entity.Property(e => e.WIDTH).HasMaxLength(50);

                entity.HasOne(d => d.GRIDPAGE)
                    .WithMany(p => p.GRIDITEM)
                    .HasForeignKey(d => d.GRIDPAGEID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GRIDITEM_GRIDITEM");
            });

            modelBuilder.Entity<GRIDPAGE>(entity =>
            {
                entity.Property(e => e.CLASSNAME).HasMaxLength(255);

                entity.Property(e => e.NAME)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TEMPLATENAME).HasMaxLength(255);
            });

            modelBuilder.Entity<LogEvents>(entity =>
            {
                entity.Property(e => e.TimeStamp).HasColumnType("datetime");
            });

            modelBuilder.Entity<PriceBrand>(entity =>
            {
                entity.Property(e => e.BrandCode).HasMaxLength(20);

                entity.Property(e => e.BrandName).HasMaxLength(50);

                entity.HasOne(d => d.AttachmentEmailTemlate)
                    .WithMany(p => p.PriceBrandAttachmentEmailTemlate)
                    .HasForeignKey(d => d.AttachmentEmailTemlateID)
                    .HasConstraintName("FK_PriceBrand_EmailTemplate1");

                entity.HasOne(d => d.EmailTemplate)
                    .WithMany(p => p.PriceBrandEmailTemplate)
                    .HasForeignKey(d => d.EmailTemplateID)
                    .HasConstraintName("FK_PriceBrand_EmailTemplate");
            });

            modelBuilder.Entity<PriceBrandConfiguration>(entity =>
            {
                entity.Property(e => e.LastSentPrice).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.LastUpdated).HasColumnType("datetime");

                entity.Property(e => e.LastUpdatedPrice).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.LastUpdatedUtc).HasColumnType("datetime");

                entity.Property(e => e.StartDate).HasColumnType("datetime");

                entity.HasOne(d => d.PriceBrand)
                    .WithMany(p => p.PriceBrandConfiguration)
                    .HasForeignKey(d => d.PriceBrandID)
                    .HasConstraintName("FK_PriceBrandConfiguration_PriceBrand");
            });

            modelBuilder.Entity<PriceBrandConfigurationHistory>(entity =>
            {
                entity.Property(e => e.LastUpdated).HasColumnType("datetime");

                entity.Property(e => e.Move).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.Price).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.RecordDate).HasColumnType("datetime");

                entity.Property(e => e.RecordDateUtc).HasColumnType("datetime");

                entity.HasOne(d => d.PriceBrandConfiguration)
                    .WithMany(p => p.PriceBrandConfigurationHistory)
                    .HasForeignKey(d => d.PriceBrandConfigurationID)
                    .HasConstraintName("FK_PriceBrandConfigurationHistory_PriceBrandConfiguration");
            });

            modelBuilder.Entity<PriceBrandFormula>(entity =>
            {
                entity.Property(e => e.CountyRate).HasColumnType("decimal(18, 6)");

                entity.Property(e => e.FederalStateRate).HasColumnType("decimal(18, 6)");

                entity.Property(e => e.Frt).HasColumnType("decimal(18, 6)");

                entity.Property(e => e.FrtSurch).HasColumnType("decimal(18, 6)");
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Markup).HasColumnType("decimal(18, 6)");

                entity.Property(e => e.Other).HasColumnType("decimal(18, 6)");

                entity.Property(e => e.StartDate).HasColumnType("datetime");

                entity.HasOne(d => d.PriceBrand)
                    .WithMany(p => p.PriceBrandFormula)
                    .HasForeignKey(d => d.PriceBrandID)
                    .HasConstraintName("FK_PriceBrandFormula_PriceBrand");
            });

            modelBuilder.Entity<PriceEmailRecipient>(entity =>
            {
                entity.Property(e => e.RecipientEmail).IsRequired();

                entity.Property(e => e.RecipientName).HasMaxLength(100);

                entity.HasOne(d => d.PriceBrand)
                    .WithMany(p => p.PriceEmailRecipient)
                    .HasForeignKey(d => d.PriceBrandID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PriceEmailRecipient_PriceBrand");
            });

            modelBuilder.Entity<PriceFaxRecipient>(entity =>
            {
                entity.Property(e => e.RecipientFax).HasMaxLength(50);

                entity.Property(e => e.RecipientName).HasMaxLength(100);

                entity.HasOne(d => d.PriceBrand)
                    .WithMany(p => p.PriceFaxRecipient)
                    .HasForeignKey(d => d.PriceBrandID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PriceFaxRecipient_PriceBrand");
            });

            modelBuilder.Entity<PriceSmsRecipient>(entity =>
            {
                entity.Property(e => e.PhoneNumber).HasMaxLength(50);

                entity.Property(e => e.RecipientName).HasMaxLength(100);
            });

            modelBuilder.Entity<PriceTask>(entity =>
            {
                entity.Property(e => e.TaskCompletedUtc).HasColumnType("datetime");

                entity.HasOne(d => d.PriceBrand)
                    .WithMany(p => p.PriceTask)
                    .HasForeignKey(d => d.PriceBrandID)
                    .HasConstraintName("FK_PriceTask_PriceBrand");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasOne(d => d.FunctionObject)
                    .WithMany(p => p.RolePermission)
                    .HasForeignKey(d => d.FunctionObjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RolePermission_FunctionObject");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.RolePermission)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_RolePermission_Role");
            });

            modelBuilder.Entity<ScheduleTask>(entity =>
            {
                entity.Property(e => e.ClassName).IsRequired();

                entity.Property(e => e.LastRunTime).HasColumnType("datetime");

                entity.Property(e => e.TaskName).IsRequired();
            });

            modelBuilder.Entity<ScheduleTaskLog>(entity =>
            {
                entity.Property(e => e.BeginDate).HasColumnType("datetime");

                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.HasOne(d => d.ScheduleTask)
                    .WithMany(p => p.ScheduleTaskLog)
                    .HasForeignKey(d => d.ScheduleTaskID)
                    .HasConstraintName("FK_TaskLog_ScheduleTask");
            });

            modelBuilder.Entity<SendLog>(entity =>
            {
                entity.Property(e => e.RunTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<SystemSettingItem>(entity =>
            {
                entity.Property(e => e.Category).HasMaxLength(200);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Name).IsRequired();

                entity.Property(e => e.Password).IsRequired();

                entity.Property(e => e.UserName).IsRequired();
            });

            modelBuilder.Entity<UsersInRole>(entity =>
            {
                entity.HasOne(d => d.Role)
                    .WithMany(p => p.UsersInRole)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_dbo.UsersInRoles_dbo.Roles_RoleId");
            });

            modelBuilder.Entity<tblAccount>(entity =>
            {
                entity.HasKey(e => e.AccountID);

                entity.Property(e => e.DeletedDate).HasColumnType("datetime");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Name).HasMaxLength(200);

                entity.Property(e => e.Password).HasMaxLength(50);
            });

            modelBuilder.Entity<tblAccountAccessList>(entity =>
            {
                entity.HasKey(e => e.EntryID);

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.tblAccountAccessList)
                    .HasForeignKey(d => d.AccountID)
                    .HasConstraintName("FK_tblAccountAccessList_tblAccount");
            });

            modelBuilder.Entity<tblAccountLoginHistory>(entity =>
            {
                entity.HasKey(e => e.LogID);

                entity.Property(e => e.ClientIP).HasMaxLength(50);

                entity.Property(e => e.Email).HasMaxLength(200);

                entity.Property(e => e.Password).HasMaxLength(50);

                entity.Property(e => e.RecordDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");
            });

            modelBuilder.Entity<tblAccountResetPassword>(entity =>
            {
                entity.HasKey(e => e.LogID);

                entity.Property(e => e.ClientIP).HasMaxLength(50);

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Email).HasMaxLength(200);

                entity.Property(e => e.EmailSentDate).HasColumnType("datetime");

                entity.Property(e => e.Guid).HasMaxLength(100);

                entity.Property(e => e.PasswordChangedDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblAwsSns>(entity =>
            {
                entity.Property(e => e.RecordDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblBouncedEmail>(entity =>
            {
                entity.Property(e => e.Action).HasMaxLength(50);

                entity.Property(e => e.Email).HasMaxLength(200);

                entity.Property(e => e.RecordCreated).HasColumnType("datetime");

                entity.Property(e => e.Source).HasMaxLength(500);

                entity.Property(e => e.Status).HasMaxLength(50);

                entity.Property(e => e.Subject).HasMaxLength(500);
            });

            modelBuilder.Entity<tblCarrierDeliveryLocations>(entity =>
            {
                entity.Property(e => e.Carrier).HasMaxLength(50);

                entity.Property(e => e.DeliveryLocation).HasMaxLength(150);

                entity.Property(e => e.FullAddress).HasMaxLength(150);

                entity.Property(e => e.GoogleCity).HasMaxLength(100);

                entity.Property(e => e.GoogleFullAddress).HasMaxLength(150);

                entity.Property(e => e.GoogleState).HasMaxLength(50);

                entity.Property(e => e.GoogleStreet).HasMaxLength(100);

                entity.Property(e => e.StartAddress).HasMaxLength(150);

                entity.Property(e => e.StartLocation).HasMaxLength(150);
            });

            modelBuilder.Entity<tblCarrierGulfstream>(entity =>
            {
                entity.Property(e => e.BOL).HasMaxLength(50);

                entity.Property(e => e.DestinationCity).HasMaxLength(50);

                entity.Property(e => e.DestinationStreet).HasMaxLength(200);

                entity.Property(e => e.Driver).HasMaxLength(50);

                entity.Property(e => e.FileName).HasMaxLength(100);

                entity.Property(e => e.Invoice).HasMaxLength(50);

                entity.Property(e => e.ProductType).HasMaxLength(50);

                entity.Property(e => e.RecordDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ShipDate).HasColumnType("datetime");

                entity.Property(e => e.Station).HasMaxLength(50);

                entity.Property(e => e.Terminal).HasMaxLength(50);

                entity.Property(e => e.ZipCode).HasMaxLength(50);
            });

            modelBuilder.Entity<tblCarrierGulfstreamLog>(entity =>
            {
                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.FileProcessedDate).HasColumnType("datetime");

                entity.Property(e => e.FileSentDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblCarrierInvoiceDBTrucking>(entity =>
            {
                entity.Property(e => e.BOL).HasMaxLength(50);

                entity.Property(e => e.BillTo).HasMaxLength(50);

                entity.Property(e => e.DestinationCity).HasMaxLength(50);

                entity.Property(e => e.DestinationStreet).HasMaxLength(50);

                entity.Property(e => e.DetailPro).HasMaxLength(50);

                entity.Property(e => e.FileName).HasMaxLength(100);

                entity.Property(e => e.FreightPrice).HasMaxLength(50);

                entity.Property(e => e.FreightRate).HasColumnType("decimal(18, 10)");

                entity.Property(e => e.Invoice).HasMaxLength(50);

                entity.Property(e => e.MasterPro).HasMaxLength(50);

                entity.Property(e => e.OtherFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Product).HasMaxLength(50);

                entity.Property(e => e.RecordDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ShipDate).HasColumnType("date");

                entity.Property(e => e.SplitLoad).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.StationCode).HasMaxLength(50);

                entity.Property(e => e.StationName).HasMaxLength(50);

                entity.Property(e => e.SurchargeFee).HasMaxLength(50);

                entity.Property(e => e.SurchargePercent).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Tolls).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<tblCarrierInvoiceDBTruckingLog>(entity =>
            {
                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.FileProcessedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");
            });

            modelBuilder.Entity<tblCarrierInvoiceESP>(entity =>
            {
                entity.Property(e => e.Address).HasMaxLength(100);

                entity.Property(e => e.BOL_s).HasMaxLength(200);

                entity.Property(e => e.BillTo).HasMaxLength(50);

                entity.Property(e => e.City).HasMaxLength(100);

                entity.Property(e => e.Date).HasColumnType("datetime");

                entity.Property(e => e.Description).HasMaxLength(200);

                entity.Property(e => e.Driver).HasMaxLength(50);

                entity.Property(e => e.DriverID).HasMaxLength(50);

                entity.Property(e => e.FileName).HasMaxLength(200);

                entity.Property(e => e.ItemCode).HasMaxLength(50);

                entity.Property(e => e.PONum).HasMaxLength(100);

                entity.Property(e => e.PrintInv).HasMaxLength(50);

                entity.Property(e => e.QB_REP)
                    .HasColumnName("QB-REP")
                    .HasMaxLength(50);

                entity.Property(e => e.RecordDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ShipToCityStateZip).HasMaxLength(50);

                entity.Property(e => e.ShipVia).HasMaxLength(50);

                entity.Property(e => e.Site).HasMaxLength(50);

                entity.Property(e => e.State).HasMaxLength(50);

                entity.Property(e => e.Template).HasMaxLength(50);

                entity.Property(e => e.TerminalID).HasMaxLength(50);

                entity.Property(e => e.TerminalName).HasMaxLength(50);

                entity.Property(e => e.Trailer).HasMaxLength(50);

                entity.Property(e => e.Truck).HasMaxLength(50);
            });

            modelBuilder.Entity<tblCarrierInvoiceESPLog>(entity =>
            {
                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.FileProcessedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");
            });

            modelBuilder.Entity<tblCarrierInvoiceProEnergy>(entity =>
            {
                entity.Property(e => e.Address).HasMaxLength(100);

                entity.Property(e => e.BOL).HasMaxLength(50);

                entity.Property(e => e.Carrier).HasMaxLength(50);

                entity.Property(e => e.City).HasMaxLength(50);

                entity.Property(e => e.Date).HasColumnType("date");

                entity.Property(e => e.HoursDumurrage).HasColumnType("decimal(18, 4)");

                entity.Property(e => e.MinimumGallons).HasMaxLength(50);

                entity.Property(e => e.Misc).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Rate).HasColumnType("decimal(18, 10)");

                entity.Property(e => e.SplitFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.State).HasMaxLength(50);

                entity.Property(e => e.StationName).HasMaxLength(50);

                entity.Property(e => e.SundayCharge).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.SurchargeAmount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Terminal).HasMaxLength(50);

                entity.Property(e => e.Tolls).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TotalAndSurcharge).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Type).HasMaxLength(50);

                entity.Property(e => e.Zip).HasMaxLength(10);
            });

            modelBuilder.Entity<tblCarrierInvoiceProEnergyLog>(entity =>
            {
                entity.Property(e => e.FileDate).HasColumnType("datetime");

                entity.Property(e => e.FileName).HasMaxLength(200);

                entity.Property(e => e.ProcessedDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblCarrierLoadDBTrucking>(entity =>
            {
                entity.Property(e => e.AtLocationTime).HasColumnType("datetime");

                entity.Property(e => e.AtRack).HasColumnType("datetime");

                entity.Property(e => e.BusinessUnit).HasMaxLength(50);

                entity.Property(e => e.CarrierOrderID).HasMaxLength(50);

                entity.Property(e => e.CustomField1).HasMaxLength(50);

                entity.Property(e => e.CustomField2).HasColumnType("datetime");

                entity.Property(e => e.CustomField3).HasMaxLength(50);

                entity.Property(e => e.CustomField4).HasMaxLength(50);

                entity.Property(e => e.CustomField5).HasMaxLength(50);

                entity.Property(e => e.CustomerAddrCity).HasMaxLength(50);

                entity.Property(e => e.CustomerAddrDescription).HasMaxLength(50);

                entity.Property(e => e.CustomerAddrLine1).HasMaxLength(50);

                entity.Property(e => e.CustomerAddrLine2).HasMaxLength(50);

                entity.Property(e => e.CustomerAddrState).HasMaxLength(50);

                entity.Property(e => e.CustomerAddrZip).HasMaxLength(50);

                entity.Property(e => e.CustomerLocNo).HasMaxLength(50);

                entity.Property(e => e.CustomerNo).HasMaxLength(50);

                entity.Property(e => e.CustomerPO).HasMaxLength(50);

                entity.Property(e => e.DeliveryDate).HasColumnType("date");

                entity.Property(e => e.DeliveryTimeEnd).HasMaxLength(50);

                entity.Property(e => e.DeliveryTimeStart).HasMaxLength(50);

                entity.Property(e => e.DispatchNotes).HasMaxLength(50);

                entity.Property(e => e.DriverID)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.FTPSCAC).HasMaxLength(50);

                entity.Property(e => e.FileName).HasMaxLength(100);

                entity.Property(e => e.FuelGradeCode).HasMaxLength(50);

                entity.Property(e => e.GrossGallons).HasColumnType("decimal(10, 2)");

                entity.Property(e => e.IsUndergroundTankIND).HasMaxLength(50);

                entity.Property(e => e.LeftLocationTime).HasColumnType("datetime");

                entity.Property(e => e.LeftRack).HasColumnType("datetime");

                entity.Property(e => e.LiftedEndTime).HasColumnType("datetime");

                entity.Property(e => e.LiftedStartTime).HasColumnType("datetime");

                entity.Property(e => e.LoadingBOL).HasMaxLength(50);

                entity.Property(e => e.LoadingNotes).HasMaxLength(50);

                entity.Property(e => e.NeedPumpIND).HasMaxLength(50);

                entity.Property(e => e.NetGallons).HasColumnType("decimal(10, 2)");

                entity.Property(e => e.OrderLine).HasMaxLength(50);

                entity.Property(e => e.ProductCode).HasMaxLength(50);

                entity.Property(e => e.ProductDescription).HasMaxLength(50);

                entity.Property(e => e.QuantityOrdered).HasColumnType("decimal(10, 2)");

                entity.Property(e => e.RackUpdateCode).HasMaxLength(50);

                entity.Property(e => e.RackUpdateField).HasMaxLength(50);

                entity.Property(e => e.RackUpdateValue).HasMaxLength(50);

                entity.Property(e => e.RecordDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.SalesOrderNumber).HasMaxLength(50);

                entity.Property(e => e.SupplierID).HasMaxLength(50);

                entity.Property(e => e.SupplierName).HasMaxLength(50);

                entity.Property(e => e.TankDescription).HasMaxLength(50);

                entity.Property(e => e.TankSerialNumber).HasMaxLength(50);

                entity.Property(e => e.TankSize).HasMaxLength(50);

                entity.Property(e => e.TerminalID).HasMaxLength(50);

                entity.Property(e => e.TerminalName).HasMaxLength(50);

                entity.Property(e => e.TimeZoneGMTOffset).HasMaxLength(50);

                entity.Property(e => e.TruckID).HasMaxLength(50);
            });

            modelBuilder.Entity<tblCarrierLoadDBTruckingLog>(entity =>
            {
                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.FileProcessedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");
            });

            modelBuilder.Entity<tblCompany>(entity =>
            {
                entity.Property(e => e.Code).HasMaxLength(10);

                entity.Property(e => e.Name).HasMaxLength(200);
            });

            modelBuilder.Entity<tblConfiguration>(entity =>
            {
                entity.Property(e => e.Parameter).HasMaxLength(50);

                entity.Property(e => e.Value).HasMaxLength(2000);
            });

            modelBuilder.Entity<tblDistributorTaxExclusion>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<tblESPTest>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.Address).HasMaxLength(50);

                entity.Property(e => e.BillTo).HasMaxLength(50);

                entity.Property(e => e.City).HasMaxLength(50);

                entity.Property(e => e.Description).HasMaxLength(50);

                entity.Property(e => e.Driver).HasMaxLength(50);

                entity.Property(e => e.Driver_).HasMaxLength(50);

                entity.Property(e => e.ItemCode).HasMaxLength(50);

                entity.Property(e => e.PrintInv).HasMaxLength(50);

                entity.Property(e => e.QB_REP).HasMaxLength(50);

                entity.Property(e => e.ShipToCityStateZip).HasMaxLength(50);

                entity.Property(e => e.ShipVia).HasMaxLength(50);

                entity.Property(e => e.Site).HasMaxLength(50);

                entity.Property(e => e.State).HasMaxLength(50);

                entity.Property(e => e.Template).HasMaxLength(50);

                entity.Property(e => e.Terminal_ID).HasMaxLength(50);

                entity.Property(e => e.Terminal_Name).HasMaxLength(50);

                entity.Property(e => e.Truck).HasMaxLength(50);
            });

            modelBuilder.Entity<tblEmailLog>(entity =>
            {
                entity.Property(e => e.EmailRecipient).HasMaxLength(100);

                entity.Property(e => e.RecordDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblEmailTemplate>(entity =>
            {
                entity.Property(e => e.EmailSubject).HasMaxLength(100);

                entity.Property(e => e.TemplateName).HasMaxLength(50);

                entity.Property(e => e.TemplateType).HasMaxLength(50);
            });

            modelBuilder.Entity<tblFaxLog>(entity =>
            {
                entity.Property(e => e.FaxNumber).HasMaxLength(50);

                entity.Property(e => e.LastStatusChecked).HasColumnType("datetime");

                entity.Property(e => e.LastStstusCheckedUtc).HasColumnType("datetime");

                entity.Property(e => e.MessageID).HasMaxLength(50);

                entity.Property(e => e.SentTimestamp).HasColumnType("datetime");

                entity.Property(e => e.Status).HasMaxLength(50);
            });

            modelBuilder.Entity<tblGasStation>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<tblGasStationAddress>(entity =>
            {
                entity.HasKey(e => e.StationID)
                    .HasName("PK_tblGasStation1");

                entity.Property(e => e.StationID).HasMaxLength(50);

                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.CarWashBrand)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.Carrier)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.City)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Manager)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.StationName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Zip)
                    .IsRequired()
                    .HasMaxLength(10);
            });

            modelBuilder.Entity<tblGasType>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(50);

                entity.Property(e => e.TaxScheduleDisbursement).HasMaxLength(50);

                entity.Property(e => e.TaxScheduleReceipts).HasMaxLength(50);
            });

            modelBuilder.Entity<tblGoogleDirectionsServiceLog>(entity =>
            {
                entity.Property(e => e.ApiRawResponse)
                    .IsRequired()
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.DestinationAddress)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.DistanceMiles).HasColumnType("decimal(18, 6)");

                entity.Property(e => e.EndAddress)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.EndAddressLatitude).HasColumnType("decimal(18, 10)");

                entity.Property(e => e.EndAddressLongitude).HasColumnType("decimal(18, 10)");

                entity.Property(e => e.ErrorMessage)
                    .IsRequired()
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.GasStationID)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.OriginAddress)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.RecordDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.RouteDistance)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.StartAddress)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.StartAddressLatitude).HasColumnType("decimal(18, 10)");

                entity.Property(e => e.StartAddressLongitude).HasColumnType("decimal(18, 10)");
            });

            modelBuilder.Entity<tblOrder>(entity =>
            {
                entity.Property(e => e.AccountNumber).HasMaxLength(50);

                entity.Property(e => e.BillTo).HasMaxLength(100);

                entity.Property(e => e.ClearDiesel).HasMaxLength(20);

                entity.Property(e => e.ClientIP).HasMaxLength(50);

                entity.Property(e => e.Comments).HasMaxLength(2000);

                entity.Property(e => e.ContactEmail).HasMaxLength(200);

                entity.Property(e => e.ContactName).HasMaxLength(100);

                entity.Property(e => e.ContactPhone).HasMaxLength(50);

                entity.Property(e => e.CustomerName).HasMaxLength(100);

                entity.Property(e => e.DeliveryDate).HasColumnType("datetime");

                entity.Property(e => e.DeliveryLocation).HasMaxLength(200);

                entity.Property(e => e.DeliveryTimeFrom).HasMaxLength(20);

                entity.Property(e => e.DeliveryTimeTo).HasMaxLength(20);

                entity.Property(e => e.DyeDiesel).HasMaxLength(20);

                entity.Property(e => e.EmailSent).HasColumnType("datetime");

                entity.Property(e => e.NonEthanolFuel).HasMaxLength(20);

                entity.Property(e => e.OrderDate).HasColumnType("datetime");

                entity.Property(e => e.OrderNumber).HasMaxLength(50);

                entity.Property(e => e.PONumber).HasMaxLength(50);

                entity.Property(e => e.RecordDate).HasColumnType("datetime");

                entity.Property(e => e.RegularFuel).HasMaxLength(20);

                entity.Property(e => e.TankSize).HasMaxLength(20);
            });

            modelBuilder.Entity<tblOrderFuelDetails>(entity =>
            {
                entity.Property(e => e.DeliveryDate).HasColumnType("date");

                entity.Property(e => e.DeliveryLocation).HasMaxLength(500);

                entity.Property(e => e.DeliveryTimeFrom).HasMaxLength(20);

                entity.Property(e => e.DeliveryTimeTo).HasMaxLength(20);
            });

            modelBuilder.Entity<tblPickupCustomer>(entity =>
            {
                entity.Property(e => e.AccountNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CustomerName)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<tblPortalAccount>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.DeletedDate).HasColumnType("datetime");

                entity.Property(e => e.Email).HasMaxLength(100);

                entity.Property(e => e.Login).HasMaxLength(50);

                entity.Property(e => e.Password).HasMaxLength(512);
            });

            modelBuilder.Entity<tblPortalApiLog>(entity =>
            {
                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.IPAddress)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Recipient)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.RecordDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");
            });

            modelBuilder.Entity<tblPortalLoginStats>(entity =>
            {
                entity.Property(e => e.ClientIP).HasMaxLength(50);

                entity.Property(e => e.LoginName).HasMaxLength(50);

                entity.Property(e => e.RecordDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblPortalResetPassword>(entity =>
            {
                entity.Property(e => e.ClientIP).HasMaxLength(50);

                entity.Property(e => e.Email).HasMaxLength(100);

                entity.Property(e => e.EmailSent).HasColumnType("datetime");

                entity.Property(e => e.HashCode).HasMaxLength(50);

                entity.Property(e => e.PasswordChanged).HasColumnType("datetime");

                entity.Property(e => e.RequestCreated).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblPriceBrand>(entity =>
            {
                entity.Property(e => e.AttachmentFile).HasMaxLength(1000);

                entity.Property(e => e.BrandCode).HasMaxLength(20);

                entity.Property(e => e.BrandName).HasMaxLength(50);

                entity.Property(e => e.EmailFrom).HasMaxLength(100);

                entity.Property(e => e.EmailReplyTo).HasMaxLength(100);

                entity.Property(e => e.EmailSubject).HasMaxLength(200);

                entity.Property(e => e.PriceFileUrl).HasMaxLength(1000);
            });

            modelBuilder.Entity<tblPriceBrandConfiguration>(entity =>
            {
                entity.Property(e => e.CityID).HasDefaultValueSql("((0))");

                entity.Property(e => e.LastSentPrice).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.LastUpdated).HasColumnType("datetime");

                entity.Property(e => e.LastUpdatedPrice).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.LastUpdatedUtc).HasColumnType("datetime");

                entity.Property(e => e.ProductID).HasDefaultValueSql("((0))");

                entity.Property(e => e.ReportVariableName).HasMaxLength(50);

                entity.Property(e => e.SupplierID).HasDefaultValueSql("((0))");

                entity.Property(e => e.TerminalID).HasDefaultValueSql("((0))");
            });

            modelBuilder.Entity<tblPriceBrandNew>(entity =>
            {
                entity.Property(e => e.AttachmentFile).HasMaxLength(1000);

                entity.Property(e => e.BrandName).HasMaxLength(50);

                entity.Property(e => e.EmailFrom).HasMaxLength(100);

                entity.Property(e => e.EmailReplyTo).HasMaxLength(100);

                entity.Property(e => e.EmailSubject).HasMaxLength(200);

                entity.Property(e => e.PriceFileUrl).HasMaxLength(1000);
            });

            modelBuilder.Entity<tblPriceEmailRecipient>(entity =>
            {
                entity.Property(e => e.RecipientEmail).HasMaxLength(100);
            });

            modelBuilder.Entity<tblPriceEmailRecipientLog>(entity =>
            {
                entity.Property(e => e.RecipientEmail).HasMaxLength(200);
            });

            modelBuilder.Entity<tblPriceFaxRecipient>(entity =>
            {
                entity.Property(e => e.RecipientFax).HasMaxLength(50);

                entity.Property(e => e.RecipientName).HasMaxLength(100);
            });

            modelBuilder.Entity<tblPriceHistory>(entity =>
            {
                entity.Property(e => e.Move).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.Price).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.RecordDate).HasColumnType("datetime");

                entity.Property(e => e.RecordDateUtc).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblPriceNotificationEmail>(entity =>
            {
                entity.Property(e => e.EmailAddress).HasMaxLength(200);
            });

            modelBuilder.Entity<tblPriceNotificationType>(entity =>
            {
                entity.Property(e => e.NotificationType).HasMaxLength(100);
            });

            modelBuilder.Entity<tblPriceProduct>(entity =>
            {
                entity.Property(e => e.ID).ValueGeneratedNever();

                entity.Property(e => e.ProductName).HasMaxLength(100);
            });

            modelBuilder.Entity<tblPriceSentHistory>(entity =>
            {
                entity.Property(e => e.LastSentUtc).HasColumnType("datetime");

                entity.Property(e => e.Price).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.RecordDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblPriceSmsRecipient>(entity =>
            {
                entity.Property(e => e.PhoneNumber).HasMaxLength(50);

                entity.Property(e => e.RecipientName).HasMaxLength(100);
            });

            modelBuilder.Entity<tblPriceTask>(entity =>
            {
                entity.Property(e => e.TaskCompletedUtc).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblSendGridHookLog>(entity =>
            {
                entity.Property(e => e.Email).HasMaxLength(200);

                entity.Property(e => e.Event).HasMaxLength(50);

                entity.Property(e => e.IP).HasMaxLength(50);

                entity.Property(e => e.RecordDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");
            });

            modelBuilder.Entity<tblSmsLog>(entity =>
            {
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

                entity.Property(e => e.MessageSID).HasMaxLength(50);

                entity.Property(e => e.RecipientPhone).HasMaxLength(50);

                entity.Property(e => e.RecordDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblSmsTemplate>(entity =>
            {
                entity.Property(e => e.SmsBody).HasMaxLength(500);

                entity.Property(e => e.TemplateName).HasMaxLength(50);
            });

            modelBuilder.Entity<tbl_upload>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.column7).HasMaxLength(1);
            });

            modelBuilder.Entity<tbl_utest>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.Price).HasColumnType("money");

                entity.Property(e => e.ReportVariableName).HasMaxLength(50);
            });

            modelBuilder.Entity<vwCarrierInvoiceSummary>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vwCarrierInvoiceSummary");

                entity.Property(e => e.Carrier)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.DeliveryLocation).HasMaxLength(50);

                entity.Property(e => e.InvoiceNumber).HasMaxLength(50);

                entity.Property(e => e.Item)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.Property(e => e.SundayDeliveryFee).HasColumnType("numeric(38, 1)");
            });

            modelBuilder.Entity<vwCarrierUniqueDeliveryLocations>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vwCarrierUniqueDeliveryLocations");

                entity.Property(e => e.Carrier).HasMaxLength(50);

                entity.Property(e => e.DeliveryLocation).HasMaxLength(150);

                entity.Property(e => e.FullAddress).HasMaxLength(150);
            });

            modelBuilder.Entity<vwDBTruckingDetail>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vwDBTruckingDetail");

                entity.Property(e => e.Carrier)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.DeliveryDate).HasColumnType("date");

                entity.Property(e => e.DeliveryFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.DeliveryLocation).HasMaxLength(50);

                entity.Property(e => e.GallonsNet).HasColumnType("numeric(1, 1)");

                entity.Property(e => e.InvoiceNumber).HasMaxLength(50);

                entity.Property(e => e.Item).HasMaxLength(50);

                entity.Property(e => e.OtherFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.RatePerGallon).HasColumnType("decimal(18, 10)");

                entity.Property(e => e.SplitFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.SundayDeliveryFee).HasColumnType("numeric(1, 1)");

                entity.Property(e => e.SurchargeFee).HasColumnType("numeric(18, 2)");

                entity.Property(e => e.SurchargePercent).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TollFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TotalFee).HasColumnType("numeric(19, 2)");

                entity.Property(e => e.TotalWithSurcharge).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<vwDeliveryDetails>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vwDeliveryDetails");

                entity.Property(e => e.BOL).HasMaxLength(50);

                entity.Property(e => e.Carrier)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.DeliveryDate).HasColumnType("date");

                entity.Property(e => e.DeliveryLocation).HasMaxLength(50);

                entity.Property(e => e.FullAddress).HasMaxLength(307);

                entity.Property(e => e.InvoiceNumber).HasMaxLength(50);

                entity.Property(e => e.TerminalID).HasMaxLength(50);
            });

            modelBuilder.Entity<vwESPDetail>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vwESPDetail");

                entity.Property(e => e.Carrier)
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.Property(e => e.DeliveryLocation).HasMaxLength(50);

                entity.Property(e => e.GallonsNet).HasColumnType("numeric(2, 1)");

                entity.Property(e => e.InvoiceNumber).HasMaxLength(50);

                entity.Property(e => e.Item)
                    .IsRequired()
                    .HasMaxLength(8)
                    .IsUnicode(false);

                entity.Property(e => e.SundayDeliveryFee).HasColumnType("numeric(38, 1)");
            });

            modelBuilder.Entity<vwGulfstreamDetail>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vwGulfstreamDetail");

                entity.Property(e => e.Carrier)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.DeliveryDate).HasColumnType("datetime");

                entity.Property(e => e.DeliveryLocation).HasMaxLength(50);

                entity.Property(e => e.InvoiceNumber).HasMaxLength(50);

                entity.Property(e => e.Item).HasMaxLength(50);

                entity.Property(e => e.OtherFee).HasColumnType("numeric(1, 1)");

                entity.Property(e => e.SundayDeliveryFee).HasColumnType("numeric(1, 1)");
            });

            modelBuilder.Entity<vwProEnergyDetail>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vwProEnergyDetail");

                entity.Property(e => e.Carrier)
                    .IsRequired()
                    .HasMaxLength(9)
                    .IsUnicode(false);

                entity.Property(e => e.DeliveryDate).HasColumnType("date");

                entity.Property(e => e.DeliveryFee).HasColumnType("decimal(29, 10)");

                entity.Property(e => e.DeliveryLocation).HasMaxLength(50);

                entity.Property(e => e.InvoiceNumber).HasMaxLength(50);

                entity.Property(e => e.Item).HasMaxLength(50);

                entity.Property(e => e.OtherFee).HasColumnType("numeric(1, 1)");

                entity.Property(e => e.RatePerGallon).HasColumnType("decimal(18, 10)");

                entity.Property(e => e.SplitFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.SundayDeliveryFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.SurchargeFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TollFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TotalFee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TotalWithSurcharge).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<vw_tblESPTest_BaseView>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_tblESPTest_BaseView");

                entity.Property(e => e.Carrier)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.Property(e => e.DeliveryLocation).HasMaxLength(50);

                entity.Property(e => e.GallonsNet).HasColumnType("numeric(1, 1)");

                entity.Property(e => e.InvoiceNumber).HasMaxLength(50);

                entity.Property(e => e.Item)
                    .IsRequired()
                    .HasMaxLength(8)
                    .IsUnicode(false);

                entity.Property(e => e.SundayDeliveryFee).HasColumnType("numeric(1, 1)");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
