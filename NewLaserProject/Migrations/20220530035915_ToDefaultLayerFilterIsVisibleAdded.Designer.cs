﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NewLaserProject.Data;

#nullable disable

namespace NewLaserProject.Migrations
{
    [DbContext(typeof(LaserDbContext))]
    [Migration("20220530035915_ToDefaultLayerFilterIsVisibleAdded")]
    partial class ToDefaultLayerFilterIsVisibleAdded
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.4");

            modelBuilder.Entity("NewLaserProject.Data.Models.DefaultLayerEntityTechnology", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("DefaultLayerFilterId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EntityType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TechnologyId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("DefaultLayerFilterId");

                    b.HasIndex("TechnologyId");

                    b.ToTable("DefaultLayerEntityTechnology");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.DefaultLayerFilter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Filter")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsVisible")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("DefaultLayerFilter");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.Material", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<float>("Thickness")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.ToTable("Material");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.Technology", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("MaterialId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProcessingProgram")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ProgramName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("MaterialId");

                    b.ToTable("Technology");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.DefaultLayerEntityTechnology", b =>
                {
                    b.HasOne("NewLaserProject.Data.Models.DefaultLayerFilter", "DefaultLayerFilter")
                        .WithMany("defaultLayerEntityTechnologies")
                        .HasForeignKey("DefaultLayerFilterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("NewLaserProject.Data.Models.Technology", "Technology")
                        .WithMany("DefaultLayerEntityTechnologies")
                        .HasForeignKey("TechnologyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DefaultLayerFilter");

                    b.Navigation("Technology");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.Technology", b =>
                {
                    b.HasOne("NewLaserProject.Data.Models.Material", "Material")
                        .WithMany("Technologies")
                        .HasForeignKey("MaterialId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Material");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.DefaultLayerFilter", b =>
                {
                    b.Navigation("defaultLayerEntityTechnologies");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.Material", b =>
                {
                    b.Navigation("Technologies");
                });

            modelBuilder.Entity("NewLaserProject.Data.Models.Technology", b =>
                {
                    b.Navigation("DefaultLayerEntityTechnologies");
                });
#pragma warning restore 612, 618
        }
    }
}