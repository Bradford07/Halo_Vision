using HaloVision.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Data;

// The backend's connection to the database
//IdentityDbContext adds the tables for athlete logins
//Each DbSet below is one of our own tables.

public class AppDbContext : IdentityDbContext<IdentityUser>
{

    //Name of the hidden column that links a profile to the account that owns it
    //Database has this column but the shared AthleteProfile class doesn't
    //so nothing server-only ends up in the code Unity compiles
   public const string UserIdColumn = "UserId";

   public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
   {
   }

   public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<PairingCode> PairingCodes => Set<PairingCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        //Identity sets up its own login tables
        base.OnModelCreating(builder);

        //athlete_profile: exactly one per account
        builder.Entity<AthleteProfile>(profile =>
        {
            profile.ToTable("athlete_profile");
            profile.HasKey(p => p.ProfileID);

            // The owning account: required and unique = one profile per account.
            profile.Property<string>(UserIdColumn).IsRequired();
            profile.HasOne<IdentityUser>().WithMany().HasForeignKey(UserIdColumn);
            profile.HasIndex(UserIdColumn).IsUnique();

             // Every save checks Version hasn't changed since it was read (safe sync, step 4).
            profile.Property(p => p.Version).IsConcurrencyToken();
        });

        // device: a paired headset, found by its token's hash.
        builder.Entity<Device>(device =>
        {
            device.ToTable("device");
            device.HasIndex(d => d.TokenHash).IsUnique();
            device.HasOne<AthleteProfile>().WithMany().HasForeignKey(d => d.ProfileID);
        });

        // pairing_code: a one-time code; no two can be the same.
        builder.Entity<PairingCode>(code =>
        {
            code.ToTable("pairing_code");
            code.HasIndex(c => c.Code).IsUnique();
            code.HasOne<AthleteProfile>().WithMany().HasForeignKey(c => c.ProfileID);
        });
    }

}