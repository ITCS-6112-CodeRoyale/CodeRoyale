using CodeRoyale.Data.Games;
using CodeRoyale.Data.Questions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeRoyale.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<QuestionSet> QuestionSets => Set<QuestionSet>();

    protected override void OnModelCreating(ModelBuilder bldr)
    {
        /* Games */
        bldr.Entity<Game>()
            .HasMany(g => g.Players)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        bldr.Entity<GamePlayer>()
            .HasMany(g => g.Results)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        /* Questions */
        bldr.Entity<Question>()
            .HasDiscriminator(q => q.Type)
            .HasValue<MultipleChoiceQuestion>(QuestionType.MultipleChoice)
            .HasValue<CodeEntryQuestion>(QuestionType.CodeEntry)
            .HasValue<BlankEntryQuestion>(QuestionType.BlankEntry);

        bldr.Entity<MultipleChoiceQuestion>();
        bldr.Entity<CodeEntryQuestion>();
        bldr.Entity<BlankEntryQuestion>();

        bldr.Entity<MultipleChoiceQuestion>()
            .HasMany(t => t.Answers)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        bldr.Entity<CodeEntryQuestion>()
            .HasMany(t => t.CorrectTests)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        bldr.Entity<CodeEntryQuestion>()
            .HasMany(t => t.IncorrectTests)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        bldr.Entity<BlankEntryQuestion>()
            .HasMany(t => t.Answers)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        bldr.Entity<CodeEntryTest>()
            .HasDiscriminator(q => q.Type)
            .HasValue<CodeEntryRegexTest>(CodeEntryTestType.Regex)
            .HasValue<CodeEntryUnitTest>(CodeEntryTestType.Unit);

        bldr.Entity<CodeEntryRegexTest>();
        bldr.Entity<CodeEntryUnitTest>();

        bldr.Entity<QuestionSet>()
            .HasMany(s => s.Questions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        bldr.Entity<QuestionSet>()
            .HasMany<Game>()
            .WithOne(g => g.QuestionSet)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(bldr);
    }
}