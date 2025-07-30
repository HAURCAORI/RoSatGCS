using AvalonDock.Layout;
using RoSatGCS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RoSatGCS.Views
{
    internal class PanesLayoutInitializerScheduler : ILayoutUpdateStrategy
    {

        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
        {

        }

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
        {

        }

        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
        {
            //AD wants to add the anchorable into destinationContainer
            //just for test provide a new anchorablepane 
            //if the pane is floating let the manager go ahead
            LayoutAnchorablePane? destPane = destinationContainer as LayoutAnchorablePane;
            if (destinationContainer != null &&
                destinationContainer.FindParent<LayoutFloatingWindow>() != null)
                return false;

            if (anchorableToShow.Content is PaneTleListViewModel)
            {
                var anchPane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(d => d.Name == "SidePane");

                if (anchPane != null)
                {
                    anchorableToShow.CanDockAsTabbedDocument = false;
                    anchorableToShow.AutoHideMinWidth = 220;
                    anchPane.DockMinWidth = 220;
                    anchPane.Children.Add(anchorableToShow);
                    return true;
                }
            }

            if (anchorableToShow.Content is PanePassTimelineViewModel)
            {
                var anchPane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(d => d.Name == "LowerPane");

                if (anchPane != null)
                {
                    anchorableToShow.CanDockAsTabbedDocument = false;
                    anchPane.DockMinHeight = 200;
                    anchorableToShow.AutoHideMinHeight = 200;
                    anchPane.Children.Add(anchorableToShow);
                    return true;
                }
            }

            return false;
        }

        public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer)
        {
            if (layout?.RootPanel == null) return false;

            if (anchorableToShow.Content is PanePassQueueViewModel)
            {
                var main = layout.Descendents().OfType<LayoutDocumentPaneGroup>().FirstOrDefault();
                if (main == null)
                {
                    main = new LayoutDocumentPaneGroup();
                    main.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    main.DockHeight = new GridLength(3, GridUnitType.Star);
                    layout.RootPanel.Children.Add(main);
                }

                var doc = new LayoutDocumentPane();
                doc.Children.Add(anchorableToShow);
                doc.DockWidth = new GridLength(4, GridUnitType.Star);
                main.Children.Add(doc);
                return true;
            }

            if (anchorableToShow.Content is PanePassScheduleViewModel)
            {
                var main = layout.Descendents().OfType<LayoutDocumentPaneGroup>().FirstOrDefault();
                if (main == null)
                {
                    main = new LayoutDocumentPaneGroup();
                    main.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    main.DockHeight = new GridLength(3, GridUnitType.Star);
                    layout.RootPanel.Children.Add(main);
                }

                var doc = new LayoutDocumentPane();
                doc.Children.Add(anchorableToShow);
                doc.DockWidth = new GridLength(3, GridUnitType.Star);
                main.Children.Add(doc);
                return true;
            }

            if (anchorableToShow.Content is PanePassCommandViewModel)
            {
                var main = layout.Descendents().OfType<LayoutDocumentPaneGroup>().FirstOrDefault();
                if (main == null)
                {
                    main = new LayoutDocumentPaneGroup();
                    main.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    main.DockHeight = new GridLength(3, GridUnitType.Star);
                    layout.RootPanel.Children.Add(main);
                }

                var doc = new LayoutDocumentPane();
                doc.Children.Add(anchorableToShow);
                doc.DockWidth = new GridLength(3, GridUnitType.Star);
                main.Children.Add(doc);
                return true;
            }


            return false;
        }
    }
}
