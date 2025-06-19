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
    internal class PanesLayoutInitializer : ILayoutUpdateStrategy
    {

        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
        {
            
        }

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
        {
            if (anchorableShown.Content is PaneTypeSummaryViewModel content)
            {

                var pane = layout.Descendents().OfType<LayoutDocumentFloatingWindow>().FirstOrDefault();

                if (pane == null)
                {
                    double screenWidth = SystemParameters.PrimaryScreenWidth;
                    double screenHeight = SystemParameters.PrimaryScreenHeight;
                    anchorableShown.FloatingWidth = 600;
                    anchorableShown.FloatingHeight = 300;
                    anchorableShown.FloatingLeft = (screenWidth - anchorableShown.FloatingWidth) / 2;
                    anchorableShown.FloatingTop = (screenHeight - anchorableShown.FloatingHeight) / 2;
                    anchorableShown.Float();
                }

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    layout.ActiveContent = anchorableShown;
                }));
            }

            if (anchorableShown.Content is PaneFunctionPropertyViewModel)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    layout.ActiveContent = anchorableShown;
                }));
            }

            if (anchorableShown.Content is PanePropertyPreviewViewModel)
            {
                
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                anchorableShown.FloatingWidth = 600;
                anchorableShown.FloatingHeight = 300;
                anchorableShown.FloatingLeft = (screenWidth - anchorableShown.FloatingWidth) / 2;
                anchorableShown.FloatingTop = (screenHeight - anchorableShown.FloatingHeight) / 2;
                anchorableShown.Float();


                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    layout.ActiveContent = anchorableShown;
                }));
                
            }
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

            if (anchorableToShow.Content is PaneCommandFileViewModel)
            {
                anchorableToShow.AutoHideWidth = 300;
                var anchPane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(d => d.Name == "CommandFilePane");
                if (anchPane != null)
                {
                    anchorableToShow.CanDockAsTabbedDocument = false;
                    anchPane.Children.Add(anchorableToShow);
                    return true;
                }
            }

            if (anchorableToShow.Content is PaneTypeDictionaryViewModel)
            {
                anchorableToShow.AutoHideWidth = 300;
                var anchPane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(d => d.Name == "TypeDictionaryPane");
                if (anchPane != null)
                {
                    anchorableToShow.CanDockAsTabbedDocument = false;
                    anchPane.Children.Add(anchorableToShow);
                    return true;
                }
            }

            return false;
        }

        public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer)
        {
            if (layout?.RootPanel == null) return false;

            if (anchorableToShow.Content is PaneFunctionListViewModel)
            {
                var main = layout.Descendents().OfType<LayoutDocumentPaneGroup>().FirstOrDefault();
                if (main == null) {
                    main = new LayoutDocumentPaneGroup();
                    main.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    main.DockHeight = new GridLength(5, GridUnitType.Star);
                    layout.RootPanel.Children.Add(main);
                }

                var doc = new LayoutDocumentPane();
                doc.Children.Add(anchorableToShow);
                doc.DockWidth = new GridLength(3, GridUnitType.Star);
                main.Children.Add(doc);
                return true;
            }

            if (anchorableToShow.Content is PaneCommandSetViewModel)
            {
                var main = layout.Descendents().OfType<LayoutDocumentPaneGroup>().FirstOrDefault();
                if (main == null)
                {
                    main = new LayoutDocumentPaneGroup();
                    main.Orientation = System.Windows.Controls.Orientation.Vertical;
                    layout.RootPanel.Children.Add(main);
                    main.DockHeight = new GridLength(5, GridUnitType.Star);
                    layout.RootPanel.Children.Add(main);
                }

                var doc = new LayoutDocumentPane();
                doc.Children.Add(anchorableToShow);
                doc.DockWidth = new GridLength(2, GridUnitType.Star);
                main.Children.Add(doc);
                return true;
            }
            
            if (anchorableToShow.Content is PaneTypeSummaryViewModel)
            {
                var pane = layout.Descendents().OfType<LayoutDocumentFloatingWindow>().FirstOrDefault();
                if (pane != null)
                {
                    var existingPane = pane.RootPanel.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                    if (existingPane == null)
                        return false;

                    existingPane.Children.Add(anchorableToShow);
                    return true;
                }
            }

            if (anchorableToShow.Content is PaneFunctionPropertyViewModel)
            {
                var main = layout.Descendents().OfType<LayoutDocumentPaneGroup>().FirstOrDefault();
                if (main == null) {
                    main = new LayoutDocumentPaneGroup();
                    main.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    main.DockHeight = new GridLength(2, GridUnitType.Star);
                    layout.RootPanel.Children.Add(main);
                }

                LayoutDocumentPane? target = null;
                foreach (var sub in main.Descendents().OfType<LayoutDocumentPaneGroup>())
                {
                    bool valid = true;
                    var tab = sub.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                    if (tab == null) continue;
                    foreach (var child in tab.Children)
                    {
                        if (child.Content is not PaneFunctionPropertyViewModel)
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (valid)
                    {
                        target = tab;
                    }
                }

                if(target != null)
                {
                    target.Children.Add(anchorableToShow);
                }
                else
                {
                    var maingroup = new LayoutDocumentPaneGroup();
                    maingroup.Orientation = System.Windows.Controls.Orientation.Vertical;
                    main.DockHeight = new GridLength(5, GridUnitType.Star);
                    maingroup.Children.Add(main);

                    var doc = new LayoutDocumentPane();
                    doc.Children.Add(anchorableToShow);

                    var group = new LayoutDocumentPaneGroup();
                    group.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    group.DockHeight = new GridLength(2, GridUnitType.Star);
                    group.DockMinHeight = 200;
                    group.Children.Add(doc);
                    maingroup.Children.Add(group);

                    layout.RootPanel.Children.Add(maingroup);
                }

                return true;
            }

            if (anchorableToShow.Content is PanePropertyPreviewViewModel)
            {
                var existingPane = layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (existingPane == null)
                {
                    var mainPane = new LayoutDocumentPane();
                    mainPane.Children.Add(anchorableToShow);
                    layout.RootPanel.Children.Add(mainPane);
                }
                else
                {
                    existingPane.Children.Add(anchorableToShow);
                }

                return true;
            }


            return false;
        }
    }
}
