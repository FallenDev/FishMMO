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
	/// Entity configuration for CharacterPetEntity with explicit indexes and constraints.
	/// </summary>
	public class CharacterPetEntityConfiguration : IEntityTypeConfiguration<CharacterPetEntity>
	{
		public void Configure(EntityTypeBuilder<CharacterPetEntity> builder)
		{
			builder.ToTable("character_pet");

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

			builder.Property(e => e.Abilities)
				.IsRequired()
				.HasConversion(
					list => JsonSerializer.Serialize(list ?? new List<int>(), (JsonSerializerOptions?)null),
					json => JsonSerializer.Deserialize<List<int>>(json, (JsonSerializerOptions?)null) ?? new List<int>())
				.Metadata.SetValueComparer(listComparer);

			builder.Property(e => e.Abilities)
				.HasColumnType("nvarchar(max)")
				.HasDefaultValue(new List<int>());

			builder.Property(e => e.Spawned)
				.IsRequired()
				.HasDefaultValue(false);

			// Unique constraint: one pet per character
			builder.HasIndex(e => e.CharacterID)
				.IsUnique();

			// Foreign key relationship
			builder.HasOne(e => e.Character)
				.WithOne(c => c.Pet)
				.HasForeignKey<CharacterPetEntity>(e => e.CharacterID)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}
}
