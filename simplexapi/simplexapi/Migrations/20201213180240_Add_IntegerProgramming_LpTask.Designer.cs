﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simplexapi.Data;

namespace simplexapi.Migrations
{
    [DbContext(typeof(SimplexSolverDbContext))]
    [Migration("20201213180240_Add_IntegerProgramming_LpTask")]
    partial class Add_IntegerProgramming_LpTask
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("simplexapi.Models.LpIterationLog", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("IterationLog")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("LpTaskId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("LpTaskId");

                    b.ToTable("LpIterationLogs");
                });

            modelBuilder.Entity("simplexapi.Models.LpTask", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("IntegerProgramming")
                        .HasColumnType("bit");

                    b.Property<string>("LPModelAsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SolutionAsJson")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("SolvedAt")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.ToTable("LpTasks");
                });

            modelBuilder.Entity("simplexapi.Models.LpIterationLog", b =>
                {
                    b.HasOne("simplexapi.Models.LpTask", "LpTask")
                        .WithMany()
                        .HasForeignKey("LpTaskId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LpTask");
                });
#pragma warning restore 612, 618
        }
    }
}
