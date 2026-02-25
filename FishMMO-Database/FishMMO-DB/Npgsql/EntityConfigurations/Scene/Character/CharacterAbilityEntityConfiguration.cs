using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishMMO.Database.SqlServer.Entities
{
	/// <summary>
	/// Entity configuration for CharacterAbilityEntity with explicit indexes and constraints.
	/// </summary>
	public class CharacterAbilityEntityConfiguration : IEntityTypeConfiguration<CharacterAbilityEntity>
	{
		public void Configure(EntityTypeBuilder<CharacterAbilityEntity> builder)
		{
			builder.ToTable("character_abilities");

			// Primary Key
			builder.HasKey(e => e.ID);

			builder.Property(e => e.ID)
				.ValueGeneratedOnAdd();

			// Required fields
			builder.Property(e => e.CharacterID)
				.IsRequired();

			builder.Property(e => e.TemplateID)
				.IsRequired();

			var listComparer = new ValueComparer<List<int>>(
				(left, right) => (left ?? new List<int>()).SequenceEqual(right ?? new List<int>()),
				list => (list ?? new List<int>()).Aggregate(0, (current, value) => HashCode.Combine(current, value)),
				list => list == null ? new List<int>() : list.ToList());

			builder.Property(e => e.AbilityEvents)
				.IsRequired()
				.HasConversion(
					list => JsonSerializer.Serialize(list ?? new List<int>(), (JsonSerializerOptions?)null),
					json => JsonSerializer.Deserialize<List<int>>(json, (JsonSerializerOptions?)null) ?? new List<int>())
				.Metadata.SetValueComparer(listComparer);

			builder.Property(e => e.AbilityEvents)
				.HasColumnType("nvarchar(max)")
				.HasDefaultValue(new List<int>());

			builder.Property(e => e.Cooldown)
				.IsRequired()
				.HasDefaultValue(0f);

			// Unique constraint: one ability template per character
			builder.HasIndex(e => new { e.CharacterID, e.TemplateID })
				.IsUnique();

			// Performance index for character ability queries
			builder.HasIndex(e => e.CharacterID);

			// Foreign key relationship
			builder.HasOne(e => e.Character)
				.WithMany(c => c.Abilities)
				.HasForeignKey(e => e.CharacterID)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}
}
