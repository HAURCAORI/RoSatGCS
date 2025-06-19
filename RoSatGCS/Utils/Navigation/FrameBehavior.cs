using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RoSatGCS.Utils.Navigation
{
    public class FrameBehavior : Behavior<Frame>
    {
        /// <summary>
        /// NavigationSource DP 변경 때문에 발생하는 프로퍼티 체인지 이벤트를 막기 위해 사용
        /// </summary>
        private bool _isWork;

        protected override void OnAttached()
        {
            AssociatedObject.Navigating += AssociatedObject_Navigating;
            AssociatedObject.Navigated += AssociatedObject_Navigated;
        }

        /// <summary>
        /// 네비게이션 종료 이벤트 핸들러
        /// </summary>n
        private void AssociatedObject_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            
            _isWork = true;
            NavigationSource = (Page) e.Content;
            _isWork = false;
            if (AssociatedObject.Content is Page pageContent && pageContent.DataContext is INavigationAware navigationAware)
            {
                navigationAware.OnNavigated(sender, e);
            }
        }
        /// <summary>
        /// 네비게이션 시작 이벤트 핸들러
        /// </summary>
        private void AssociatedObject_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (AssociatedObject.Content is Page pageContent && pageContent.DataContext is INavigationAware navigationAware)
            {
                navigationAware?.OnNavigating(sender, e);
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Navigating -= AssociatedObject_Navigating;
            AssociatedObject.Navigated -= AssociatedObject_Navigated;
        }

        public Page NavigationSource
        {
            get { return (Page)GetValue(NavigationSourceProperty); }
            set { SetValue(NavigationSourceProperty, value); }
        }

        /// <summary>
        /// NavigationSource DP
        /// </summary>
        public static readonly DependencyProperty NavigationSourceProperty =
            DependencyProperty.Register(nameof(NavigationSource), typeof(Page), typeof(FrameBehavior), new PropertyMetadata(null, NavigationSourceChanged));

        /// <summary>
        /// NavigationSource PropertyChanged
        /// </summary>
        private static void NavigationSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (FrameBehavior)d;
            if (behavior._isWork)
            {
                return;
            }
            behavior.Navigate();
        }

        /// <summary>
        /// 네비게이트
        /// </summary>
        private void Navigate()
        {
            if (NavigationSource != null)
            {
                AssociatedObject.Content = NavigationSource;
            }
        }
    }
}
