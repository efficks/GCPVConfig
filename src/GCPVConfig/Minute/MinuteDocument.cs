﻿using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GCPVConfig.Minute
{
    static class SimpleExtension
    {
        private static IContainer Cell(this IContainer container, bool dark)
        {
            return container
                .Border(0)
                .Padding(1);
        }

        // allows you to inject any type of content, e.g. image
        public static IContainer ValueCell(this IContainer container) => container.Cell(false);
    }
    internal abstract class Event
    {
        private TimeOnly mDebut;
        private TimeSpan mDuree;

        public TimeOnly Debut { get { return mDebut; } }
        public TimeSpan Duree { get { return mDuree; } }
        public TimeOnly Fin
        {
            get
            {
                return mDebut.Add(mDuree);
            }
        }

        public Event(TimeOnly debut, TimeSpan duree)
        {
            mDebut = debut;
            mDuree = duree;
        }
        public virtual void Compose(TableDescriptor table)
        {
            table.Cell().ColumnSpan(6);
        }

        public static IContainer Block(IContainer container)
        {
            return container
                .Border(0)
                //.MinWidth(50)
                //.MinHeight(50)
                .Padding(0)
                .Shrink()
                .AlignLeft()
                .AlignBottom();
        }

    }

    internal class NoGroupEvent : Event
    {
        private string mNom;
        private bool mBold;
        public NoGroupEvent(string name, TimeOnly debut, TimeSpan duree, bool bold=false):
            base(debut, duree)
        {
            mNom = name;
            mBold = bold;
        }

        public override void Compose(TableDescriptor table)
        {
            table.Cell().ValueCell().Element(Event.Block).Text(Debut.ToShortTimeString()).FontFamily("Times New Roman").FontSize(10);
            if (Duree == TimeSpan.Zero)
            {
                var component = table.Cell().ColumnSpan(5).ValueCell().Element(Event.Block).Text(mNom).FontFamily("Times New Roman").FontSize(10);
                if(mBold)
                {
                    component.Bold();
                }
            }
            else
            {
                var component = table.Cell().ColumnSpan(4).ValueCell().Element(Event.Block).Text(mNom).FontFamily("Times New Roman").FontSize(10);
                table.Cell().ValueCell().Element(Event.Block).Text(
                    $"{Convert.ToInt32(Duree.Hours)}:{Duree.Minutes.ToString().PadLeft(2, '0')}"
                ).FontFamily("Times New Roman").FontSize(10);
                if (mBold)
                {
                    component.Bold();
                }
            }
        }

    }

    internal class EventGlace : Event
    {
        private string mNom;
        private string mGroupe;
        private string mNombre;
        private string mQualifCrit;
        public EventGlace(TimeOnly debut, TimeSpan duree, string nom, string groupe, string nombre, string qualifCrit) :
            base(debut, duree)
        {
            mNom = nom;
            mGroupe = groupe;
            mNombre = nombre;
            mQualifCrit = qualifCrit;
        }

        public override void Compose(TableDescriptor table)
        {
            table.Cell().ValueCell().Element(Event.Block).Text(Debut.ToShortTimeString()).FontFamily("Times New Roman").FontSize(10);
            table.Cell().ValueCell().Element(Event.Block).Text(mNom).FontFamily("Times New Roman").FontSize(10);
            table.Cell().ValueCell().Element(Event.Block).Text(mGroupe).FontFamily("Times New Roman").FontSize(10);
            table.Cell().ValueCell().Element(Event.Block).Text(mNombre).FontFamily("Times New Roman").FontSize(10);
            table.Cell().ValueCell().Element(Event.Block).Text(mQualifCrit).FontFamily("Times New Roman").FontSize(10);
            table.Cell().ValueCell().Element(Event.Block).Text(
                $"{Convert.ToInt32(Duree.Hours)}:{Duree.Minutes.ToString().PadLeft(2,'0')}"
            ).FontFamily("Times New Roman").FontSize(10);
        }

    }

    internal class MinuteDocument : IDocument
    {
        private FichierPAT.Competition mCompetition;
        private List<Event> mEvenements;
        public MinuteDocument(FichierPAT.Competition compe, List<Event> evenements)
        {
            mCompetition = compe;
            mEvenements = evenements;
        }

        public DocumentMetadata GetMetadata()
        {
            var metadata = DocumentMetadata.Default;
            metadata.Title = mCompetition.Nom;
            return metadata;
        }
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginTop(50);
                page.MarginBottom(50);
                page.MarginLeft(100);
                page.MarginRight(100);
                page.Content().Element(ComposeContent);

                page.Footer().AlignRight().Text(x =>
                {
                    var style = TextStyle.Default.FontSize(10).FontFamily("Arial");
                    x.CurrentPageNumber().Style(style);
                    x.Span(" / ").Style(style);
                    x.TotalPages().Style(style);
                });
            });
        }

        private static string ToTitleCase(string str)
        {
            var firstword = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.Split(' ')[0].ToLower());
            str = str.Replace(str.Split(' ')[0], firstword);
            return str;
        }

        void ComposeContent(IContainer container)
        {
            //Title
            container.Column(col =>
            {
                col.Item().AlignCenter()
                    .AlignTop()
                    .Text($"{mCompetition.Nom}").Style(TextStyle.Default.FontSize(16).FontFamily("Arial").Bold());

                col.Item().AlignCenter()
                    .AlignTop()
                    .Text($"{mCompetition.Club.Name.ToUpper()}").Style(TextStyle.Default.FontSize(16).FontFamily("Arial").Bold());

                col.Item().Border(0).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(60);
                        columns.RelativeColumn(125);
                        columns.RelativeColumn(160);
                        columns.RelativeColumn(80);
                        columns.RelativeColumn(60);
                        columns.RelativeColumn(60);
                    });
                    CultureInfo culture = new CultureInfo("fr-CA");
                    string date = mCompetition.Date.ToString("dddd d MMMM yyyy", culture);
                    date = ToTitleCase(date);
                    table.Cell().ColumnSpan(6).ValueCell().Element(Event.Block).Text(date)
                        .FontFamily("Times New Roman").FontSize(10).Bold();

                    foreach (Event e in mEvenements)
                    {
                        e.Compose(table);
                    }
                });

            });
        }
    }
}
