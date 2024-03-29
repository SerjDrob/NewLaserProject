﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NewLaserProject.Data;

#nullable disable

namespace NewLaserProject.Migrations.WorkTimeDb
{
    [DbContext(typeof(WorkTimeDbContext))]
    partial class WorkTimeDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.2");

            modelBuilder.Entity("NewLaserProject.Data.Models.ProcTimeLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("ExceptionMessage")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .HasColumnType("TEXT");

                    b.Property<string>("MaterialName")
                        .HasColumnType("TEXT");

                    b.Property<double>("MaterialThickness")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Success")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TechnologyName")
                        .HasColumnType("TEXT");

                    b.Property<int>("WorkTimeLogId")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan>("YieldTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("WorkTimeLogId");

                    b.ToTable("ProcTimeLogs");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.WorkTimeLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("ExceptionMessage")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("WorkTimeLogs");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.ProcTimeLog", b =>
                {
                    b.HasOne("NewLaserProject.Data.Models.WorkTimeLog", "WorkTimeLog")
                        .WithMany("ProcTimeLogs")
                        .HasForeignKey("WorkTimeLogId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("WorkTimeLog");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.WorkTimeLog", b =>
                {
                    b.Navigation("ProcTimeLogs");
                });
#pragma warning restore 612, 618
        }
    }
}
