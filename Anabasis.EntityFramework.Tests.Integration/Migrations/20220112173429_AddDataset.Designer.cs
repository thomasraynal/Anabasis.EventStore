﻿// <auto-generated />
using System;
using Anabasis.EntityFramework.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Anabasis.EntityFramework.Tests.Integration.Migrations
{
    [DbContext(typeof(TestDbContext))]
    [Migration("20220112173429_AddDataset")]
    partial class AddDataset
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.13")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Anabasis.EntityFramework.Tests.Integration.Counterparty", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Name");

                    b.ToTable("Counterparties");

                    b.HasData(
                        new
                        {
                            Name = "Bank Of America"
                        },
                        new
                        {
                            Name = "Nomura"
                        },
                        new
                        {
                            Name = "HSBC"
                        },
                        new
                        {
                            Name = "SGCIB"
                        });
                });

            modelBuilder.Entity("Anabasis.EntityFramework.Tests.Integration.Currency", b =>
                {
                    b.Property<string>("Code")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Code");

                    b.HasIndex("Name", "Code")
                        .IsUnique()
                        .HasFilter("[Name] IS NOT NULL");

                    b.ToTable("Currencies");

                    b.HasData(
                        new
                        {
                            Code = "USD",
                            Name = "United States dollar"
                        },
                        new
                        {
                            Code = "EUR",
                            Name = "Euro"
                        },
                        new
                        {
                            Code = "JPY",
                            Name = "Japanese yen"
                        },
                        new
                        {
                            Code = "GBP",
                            Name = "Pound sterling"
                        },
                        new
                        {
                            Code = "AUD",
                            Name = "Australian dollar"
                        },
                        new
                        {
                            Code = "CAD",
                            Name = "Canadian dollar"
                        },
                        new
                        {
                            Code = "CHF",
                            Name = "Swiss franc"
                        },
                        new
                        {
                            Code = "CNY",
                            Name = "Renminbi"
                        },
                        new
                        {
                            Code = "HKD",
                            Name = "Hong Kong dollar"
                        },
                        new
                        {
                            Code = "NZD",
                            Name = "New Zealand dollar"
                        },
                        new
                        {
                            Code = "SEK",
                            Name = "Swedish krona"
                        },
                        new
                        {
                            Code = "KRW",
                            Name = "South Korean won"
                        },
                        new
                        {
                            Code = "SGD",
                            Name = "Singapore dollar"
                        },
                        new
                        {
                            Code = "NOK",
                            Name = "Norwegian krone"
                        },
                        new
                        {
                            Code = "MXN",
                            Name = "Mexican peso"
                        },
                        new
                        {
                            Code = "INR",
                            Name = "Indian rupee"
                        },
                        new
                        {
                            Code = "RUB",
                            Name = "Russian ruble"
                        },
                        new
                        {
                            Code = "ZAR",
                            Name = "South African rand"
                        },
                        new
                        {
                            Code = "TRY",
                            Name = "Turkish lira"
                        },
                        new
                        {
                            Code = "BRL",
                            Name = "Brazilian real"
                        },
                        new
                        {
                            Code = "TWD",
                            Name = "New Taiwan dollar"
                        },
                        new
                        {
                            Code = "DKK",
                            Name = "Danish krone"
                        },
                        new
                        {
                            Code = "PLN",
                            Name = "Polish złoty"
                        },
                        new
                        {
                            Code = "THB",
                            Name = "Thai baht"
                        },
                        new
                        {
                            Code = "IDR",
                            Name = "Indonesian rupiah"
                        },
                        new
                        {
                            Code = "HUF",
                            Name = "Hungarian forint"
                        },
                        new
                        {
                            Code = "CZK",
                            Name = "Czech koruna"
                        },
                        new
                        {
                            Code = "ILS",
                            Name = "Israeli new shekel"
                        },
                        new
                        {
                            Code = "CLP",
                            Name = "Chilean peso"
                        },
                        new
                        {
                            Code = "PHP",
                            Name = "Philippine peso"
                        },
                        new
                        {
                            Code = "AED",
                            Name = "UAE dirham"
                        },
                        new
                        {
                            Code = "COP",
                            Name = "Colombian peso"
                        },
                        new
                        {
                            Code = "SAR",
                            Name = "Saudi riyal"
                        },
                        new
                        {
                            Code = "MYR",
                            Name = "Malaysian ringgit"
                        },
                        new
                        {
                            Code = "RON",
                            Name = "Romanian leu"
                        });
                });

            modelBuilder.Entity("Anabasis.EntityFramework.Tests.Integration.CurrencyPair", b =>
                {
                    b.Property<string>("Code")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Currency1Code")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Currency2Code")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("DecimalPlaces")
                        .HasColumnType("int");

                    b.Property<decimal>("TickFrequency")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Code");

                    b.HasIndex("Currency2Code");

                    b.HasIndex("Currency1Code", "Currency2Code")
                        .IsUnique()
                        .HasFilter("[Currency1Code] IS NOT NULL AND [Currency2Code] IS NOT NULL");

                    b.ToTable("CurrencyPairs");

                    b.HasData(
                        new
                        {
                            Code = "EUR/USD",
                            Currency1Code = "EUR",
                            Currency2Code = "USD",
                            DecimalPlaces = 4,
                            TickFrequency = 3m
                        },
                        new
                        {
                            Code = "EUR/GBP",
                            Currency1Code = "EUR",
                            Currency2Code = "GBP",
                            DecimalPlaces = 4,
                            TickFrequency = 3m
                        },
                        new
                        {
                            Code = "EUR/JPY",
                            Currency1Code = "EUR",
                            Currency2Code = "JPY",
                            DecimalPlaces = 4,
                            TickFrequency = 3m
                        },
                        new
                        {
                            Code = "USD/GBP",
                            Currency1Code = "USD",
                            Currency2Code = "GBP",
                            DecimalPlaces = 4,
                            TickFrequency = 3m
                        },
                        new
                        {
                            Code = "USD/JPY",
                            Currency1Code = "USD",
                            Currency2Code = "JPY",
                            DecimalPlaces = 4,
                            TickFrequency = 3m
                        },
                        new
                        {
                            Code = "GBP/JPY",
                            Currency1Code = "GBP",
                            Currency2Code = "JPY",
                            DecimalPlaces = 4,
                            TickFrequency = 3m
                        });
                });

            modelBuilder.Entity("Anabasis.EntityFramework.Tests.Integration.Trade", b =>
                {
                    b.Property<Guid>("TradeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("Amount")
                        .HasColumnType("bigint");

                    b.Property<string>("BuyOrSell")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CounterpartyCode")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CurrencyPairCode")
                        .HasColumnType("nvarchar(450)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("TimestampUtc")
                        .HasColumnType("datetime2");

                    b.HasKey("TradeId");

                    b.HasIndex("CounterpartyCode");

                    b.HasIndex("CurrencyPairCode");

                    b.ToTable("Trades");
                });

            modelBuilder.Entity("Anabasis.EntityFramework.Tests.Integration.CurrencyPair", b =>
                {
                    b.HasOne("Anabasis.EntityFramework.Tests.Integration.Currency", "Currency1")
                        .WithMany()
                        .HasForeignKey("Currency1Code");

                    b.HasOne("Anabasis.EntityFramework.Tests.Integration.Currency", "Currency2")
                        .WithMany()
                        .HasForeignKey("Currency2Code");

                    b.Navigation("Currency1");

                    b.Navigation("Currency2");
                });

            modelBuilder.Entity("Anabasis.EntityFramework.Tests.Integration.Trade", b =>
                {
                    b.HasOne("Anabasis.EntityFramework.Tests.Integration.Counterparty", "Counterparty")
                        .WithMany()
                        .HasForeignKey("CounterpartyCode");

                    b.HasOne("Anabasis.EntityFramework.Tests.Integration.CurrencyPair", "CurrencyPair")
                        .WithMany()
                        .HasForeignKey("CurrencyPairCode");

                    b.Navigation("Counterparty");

                    b.Navigation("CurrencyPair");
                });
#pragma warning restore 612, 618
        }
    }
}
