// Written with Claude Code, reviewed by Owen Cespon
using HaloVision.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HaloVision.Api.Data;

// The backend's connection to the database.
// IdentityDbContext adds the tables ASP.NET Core Identity needs for guide accounts (AspNetUsers and friends).
// Each DbSet below is one of our own tables.
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    // Name of the hidden column that links an athlete profile to its guide.
    // It's a "shadow property": the database has it, but the shared AthleteProfile class doesn't,
    // so nothing server-only leaks into the code Unity compiles.
    public const string GuideIdColumn = "GuideId";

    // EF Core passes in the settings (like "use SQLite, file halovision.db") from Program.cs.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // P3's athlete_profile table, using the shared AthleteProfile class directly.
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();

    // Paired headsets.
    public DbSet<Device> Devices => Set<Device>();

    // One-time pairing codes.
    public DbSet<PairingCode> PairingCodes => Set<PairingCode>();

    // Rules that apply to every table at once.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite stores dates as plain text with no time zone. We always save UTC, so every date
        // read back is marked UTC too; then the JSON says "...Z" and the headset knows it's UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    // Leaves the date unchanged on the way in; marks it as UTC on the way out.
    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    // Describes the tables to EF Core: names, keys, links between tables, and how values are stored.
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Let Identity set up its own tables first.
        base.OnModelCreating(builder);

        builder.Entity<AthleteProfile>(profile =>
        {
            profile.ToTable("athlete_profile");
            profile.HasKey(p => p.ProfileID);

            // The guide who owns this profile. Required, and deleting a guide deletes their athletes.
            profile.Property<string>(GuideIdColumn).IsRequired();
            profile.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(GuideIdColumn)
                .OnDelete(DeleteBehavior.Cascade);
            profile.HasIndex(GuideIdColumn);

            // Version is a concurrency token: when EF saves a profile it adds
            // "WHERE Version = <the value we loaded>" to the UPDATE. If someone else saved in between,
            // no row matches and EF throws, so two saves can never silently overwrite each other (step 4).
            profile.Property(p => p.Version).IsConcurrencyToken();

            // Store enums as text ("Peripheral") instead of numbers (1), so the database is readable
            // and reordering an enum later can't silently change saved data.
            profile.Property(p => p.VisionLossType).HasConversion<string>();
            profile.Property(p => p.Severity).HasConversion<string>();
            profile.Property(p => p.AffectedEye).HasConversion<string>();
            profile.Property(p => p.SkillLevel).HasConversion<string>();
            profile.Property(p => p.SurfaceMode).HasConversion<string>();
            profile.Property(p => p.CueMode).HasConversion<string>();
            profile.Property(p => p.ColorPalette).HasConversion<string>();
        });

        builder.Entity<Device>(device =>
        {
            device.ToTable("device");

            // Headsets log in by token, so finding a device by its token hash must be fast and unique.
            device.HasIndex(d => d.TokenHash).IsUnique();

            // Each device belongs to one athlete profile; deleting the profile removes its devices.
            device.HasOne<AthleteProfile>()
                .WithMany()
                .HasForeignKey(d => d.ProfileID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PairingCode>(code =>
        {
            code.ToTable("pairing_code");

            // No two codes can be the same, so a code always points to exactly one profile.
            code.HasIndex(c => c.Code).IsUnique();

            code.HasOne<AthleteProfile>()
                .WithMany()
                .HasForeignKey(c => c.ProfileID)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
