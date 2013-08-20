//  Copyright 2011  Clancey
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
using System;
using MonoTouch.UIKit;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using System.Drawing;
using MonoTouch.ObjCRuntime;


namespace FlyoutNavigation
{
    [Register("FlyoutNavigationController")]
	public class FlyoutNavigationController : UIViewController
	{
		UIColor tintColor;

		public UIColor TintColor {
			get{ return tintColor;}
			set { 
				if (tintColor == value)
					return;
				SearchBar.TintColor = value;
			}
		}
		
		DialogViewController navigation;
		public UISearchBar SearchBar;

		public Action SelectedIndexChanged { get; set; }
		const float sidebarFlickVelocity = 1000.0f;
		public const int menuWidth = 250;
        public int menuHeight = 350;
		private UIView shadowView;
		private UIButton closeButton;

            private UIViewController targetViewController;

            public UIViewController TargetViewController
            { 
              get { return this.targetViewController; }
              set 
              {
                this.targetViewController = value;
                this.targetViewController.View.AddGestureRecognizer(new OpenMenuGestureRecognizer(this, new Selector("panned"), this));
              }
            }

        public enum FlyoutMode
        {
            Left,
            Top,
            Right
        }

        private FlyoutMode m_FlyMode = FlyoutMode.Left;
        public FlyoutMode FlyMode
        {
            get { return m_FlyMode; }
            set
            {
                m_FlyMode = value;
                var navFrame = navigation.View.Frame;
                switch (m_FlyMode)
                {
                    case FlyoutMode.Left:
                        navFrame.Width = menuWidth;
                        navFrame.Height = this.View.Frame.Height;
                        navFrame.X = 0;
                        AlwaysShowLandscapeMenu = true;
                        this.NavigationTableView.ContentOffset = new PointF(0, 0);
                        break;
                    case FlyoutMode.Top:
                        navFrame.Width = this.View.Frame.Width;
                        menuHeight = (int)(this.View.Frame.Height * .7f);//70% of screen
                        navFrame.Height = menuHeight;
                        navFrame.X = 0;
                        this.NavigationTableView.ContentOffset = new PointF(0, 0);
                        AlwaysShowLandscapeMenu = false;
                        break;
                    case FlyoutMode.Right:
                        navFrame.Width = menuWidth;
                        navFrame.Height = this.View.Frame.Height;
                        AlwaysShowLandscapeMenu = true;
                        break;
                }

                navigation.View.Frame = navFrame;
            }
        }

		public bool AlwaysShowLandscapeMenu { get; set; }

		public bool ForceMenuOpen { get; set; }

		public bool HideShadow{ get; set; }

		public bool GestureToOpen{ get; set; }

		public bool GestureToClose{ get; set; }

        /// <summary>
        /// When setting the ViewControllers the first view is automatically displayed. Settings this to false stops that
        /// </summary>
        /// <value><c>true</c> if should auto push first view; otherwise, <c>false</c>.</value>
        public bool AutoPushFirstView { get; set; }

		public UIViewController CurrentViewController{ get; private set; }

		UIView mainView {
			get {
				if (CurrentViewController == null)
					return null;
				return CurrentViewController.View;
			}
		}

        public FlyoutNavigationController (IntPtr handle) : base(handle)
        {
            Initialize();
        }

		public FlyoutNavigationController (UITableViewStyle navigationStyle = UITableViewStyle.Plain)
		{
            Initialize(navigationStyle);
		}

        private void Initialize (UITableViewStyle navigationStyle = UITableViewStyle.Plain)
        {
            AutoPushFirstView = true;
            navigation = new DialogViewController(navigationStyle,null);
            navigation.OnSelection += NavigationItemSelected;
            var navFrame = navigation.View.Frame;
            navFrame.Width = menuWidth;
            navigation.View.Frame = navFrame;
            this.View.AddSubview (navigation.View);
            SearchBar = new UISearchBar (new RectangleF (0, 0, navigation.TableView.Bounds.Width, 44)) {
                //Delegate = new SearchDelegate (this),
                TintColor = this.TintColor
            };

            GestureToOpen = true;
			GestureToClose = true;
            
            TintColor = UIColor.Black;
            //navigation.TableView.TableHeaderView = SearchBar;
            navigation.TableView.TableFooterView = new UIView (new RectangleF (0, 0, 100, 100)){BackgroundColor = UIColor.Clear};
            navigation.TableView.ScrollsToTop = false;
            shadowView = new UIView ();
            shadowView.BackgroundColor = UIColor.White;
            shadowView.Layer.ShadowOffset = new System.Drawing.SizeF (-5, -1);
            shadowView.Layer.ShadowColor = UIColor.Black.CGColor;
            shadowView.Layer.ShadowOpacity = .75f;
            closeButton = new UIButton ();
            closeButton.TouchUpInside += delegate {
				HideMenu ();
            };
            AlwaysShowLandscapeMenu = true;
            
            //this.View.AddGestureRecognizer (new OpenMenuGestureRecognizer (this, new Selector ("panned"), this));
        }

		public event UITouchEventArgs ShouldReceiveTouch;

		internal bool shouldReceiveTouch (UIGestureRecognizer gesture, UITouch touch)
		{
			if (ShouldReceiveTouch != null)
				return ShouldReceiveTouch (gesture, touch);
			return true;
		}

		public override void ViewDidLayoutSubviews ()
		{
			base.ViewDidLayoutSubviews ();		
			var navFrame =  this.View.Bounds;
//			navFrame.Y += UIApplication.SharedApplication.StatusBarFrame.Height;
//			navFrame.Height -= navFrame.Y;
			//this.statusbar
            if (m_FlyMode == FlyoutMode.Left)
            {
                navFrame.Width = menuWidth;
                navFrame.X = 0;
            }
            else if (m_FlyMode == FlyoutMode.Right)
            {
                navFrame.X = this.View.Frame.Width - menuWidth;
            }
            else if (m_FlyMode == FlyoutMode.Top)
            {
                navFrame.Height = menuHeight;
                navFrame.X = 0;
            }

			if (navigation.View.Frame != navFrame)
				navigation.View.Frame = navFrame;
		}

		private float startX = 0;
        private float startY = 0;
		[Export("panned")]
		public void DragContentView (UIPanGestureRecognizer panGesture)
		{
            if (ShouldStayOpen || mainView == null)
                return;
            switch (m_FlyMode)
            {
                case FlyoutMode.Left:
                    PanLeft(panGesture);
                    break;
                case FlyoutMode.Top:
                    PanTop(panGesture);
                    break;
                case FlyoutMode.Right:
                    PanRight(panGesture);
                    break;
            }
         
		}

        private void PanRight(UIPanGestureRecognizer panGesture)
        {
            var frame = mainView.Frame;
            var translation = panGesture.TranslationInView(View).X;
            //Console.WriteLine (translation);

            if (panGesture.State == UIGestureRecognizerState.Began)
            {
                startX = frame.X;
            }
            else if (panGesture.State == UIGestureRecognizerState.Changed)
            {
                //don't allow getsture to close
                if (!GestureToClose && (translation + startX) > startX)
                    translation = 0;

                //don't allow gesture to open
                if (!GestureToOpen && (translation + startX) < startX)
                    translation = 0;

                frame.X = translation + startX;
                if (frame.X < -menuWidth)
                    frame.X = -menuWidth;
                else if (frame.X > 0)
                    frame.X = 0;

                SetLocation(frame);
            }
            else if (panGesture.State == UIGestureRecognizerState.Ended)
            {
                var velocity = panGesture.VelocityInView(View).X;
                //Console.WriteLine (velocity);
                var newX = translation + startX;
                Console.WriteLine(translation + startX);
                bool show = (Math.Abs(velocity) > sidebarFlickVelocity)
                        ? (velocity > 0)
                        : startX < menuWidth ? (newX > (menuWidth / 2)) : newX > menuWidth;
                show = !show;//swap
                //don't allow flick open
                if (show && !GestureToOpen)
                    return;
                //don't allow flick close
                if (!show && !GestureToClose)
                    return;

                if (show)
                    ShowMenu();
                else
                    HideMenu();

            }
        }



        private void PanLeft(UIPanGestureRecognizer panGesture)
        {
               var frame = mainView.Frame;
            var translation = panGesture.TranslationInView(View).X;
            //Console.WriteLine (translation);

            if (panGesture.State == UIGestureRecognizerState.Began)
            {
                startX = frame.X;
            }
            else if (panGesture.State == UIGestureRecognizerState.Changed)
            {
                //don't allow getsture to close
                if (!GestureToClose && (translation + startX) < startX)
                    translation = 0;

                //don't allow gesture to open
                if (!GestureToOpen && (translation + startX) > startX)
                    translation = 0;

                frame.X = translation + startX;
                if (frame.X < 0)
                    frame.X = 0;
                else if (frame.X > frame.Width)
                    frame.X = menuWidth;
                SetLocation(frame);
            }
            else if (panGesture.State == UIGestureRecognizerState.Ended)
            {
                var velocity = panGesture.VelocityInView(View).X;
                //Console.WriteLine (velocity);
                var newX = translation + startX;
                Console.WriteLine(translation + startX);
                bool show = (Math.Abs(velocity) > sidebarFlickVelocity)
                    ? (velocity > 0)
                        : startX < menuWidth ? (newX > (menuWidth / 2)) : newX > menuWidth;

                //don't allow flick open
                if (show && !GestureToOpen)
                    return;
                //don't allow flick close
                if (!show && !GestureToClose)
                    return;

                if (show)
                    ShowMenu();
                else
                    HideMenu();

            }
        }

        private void PanTop(UIPanGestureRecognizer panGesture)
        {
            var frame = mainView.Frame;
			var translation = panGesture.TranslationInView (View).Y;
			//Console.WriteLine (translation);

			if (panGesture.State == UIGestureRecognizerState.Began) {
				startY = frame.Y;
			} else if (panGesture.State == UIGestureRecognizerState.Changed) {

                //don't allow getsture to close
                if (!GestureToClose && (translation + startY) < startY)
                    translation = 0;

                //don't allow gesture to open
                if (!GestureToOpen && (translation + startY) > startY)
                    translation = 0;

				frame.Y = translation + startY;
				if (frame.Y < 0)
					frame.Y = 0;
				else if (frame.Y > frame.Height)
					frame.Y = menuHeight;
				SetLocation(frame);
			} else if (panGesture.State == UIGestureRecognizerState.Ended) {
				var velocity = panGesture.VelocityInView(View).Y;
				//Console.WriteLine (velocity);
				var newY = translation + startY;
				Console.WriteLine (translation + startY);
				bool show = (Math.Abs(velocity) > sidebarFlickVelocity)
					? (velocity > 0)
						: startY < menuHeight ? (newY > (menuHeight / 2)) : newY > menuHeight;

                //don't allow flick open
                if (show && !GestureToOpen)
                    return;
                //don't allow flick close
                if (!show && !GestureToClose)
                    return;

				if(show)
					ShowMenu();
				else 
					HideMenu();

			}
        }
		bool firstLaunch = true;

		public override void ViewWillAppear (bool animated)
		{			
			var navFrame = navigation.View.Frame;
            if (m_FlyMode == FlyoutMode.Left || m_FlyMode == FlyoutMode.Right)
                navFrame.Width = menuWidth;
            else if (m_FlyMode == FlyoutMode.Top)
                navFrame.Height = menuHeight;

			navFrame.Location = PointF.Empty;
			navigation.View.Frame = navFrame;
			this.View.BackgroundColor = NavigationTableView.BackgroundColor;
			base.ViewWillAppear (animated);
		}
		
		public RootElement NavigationRoot {
			get{ return navigation.Root;}
			set {
				EnsureInvokedOnMainThread (delegate {
					navigation.Root = value;
				});
			}
		}

		public UITableView NavigationTableView {
			get{ return navigation.TableView;}
		}

		protected UIViewController[] viewControllers;

        public UIViewController[] ViewControllers
        {
            get { return viewControllers; }
            set
            {
                EnsureInvokedOnMainThread(delegate
                {
                    viewControllers = value;
                    if (AutoPushFirstView)
                    {
                        NavigationItemSelected(GetIndexPath(SelectedIndex));
                    }
                });
            }
        }
		
		protected void NavigationItemSelected (NSIndexPath indexPath)
		{
			var index = GetIndex (indexPath);
			NavigationItemSelected (index);
			
		}

		protected void NavigationItemSelected (int index)
		{
			selectedIndex = index;			
			if (viewControllers == null || viewControllers.Length <= index || index < 0) {
				if (SelectedIndexChanged != null)
					SelectedIndexChanged ();
				return;
			}
			if (ViewControllers [index] == null) {
				if (SelectedIndexChanged != null)
					SelectedIndexChanged ();
				return;
			}

			var isOpen = false;
			
			if (mainView != null) {
				mainView.RemoveFromSuperview ();
				isOpen = IsOpen;
			}
			CurrentViewController = ViewControllers [SelectedIndex];
			var frame = View.Bounds;
		    if (isOpen || ShouldStayOpen)
		    {
                if (m_FlyMode == FlyoutMode.Left)
                    frame.X = menuWidth;
                else if (m_FlyMode == FlyoutMode.Right)
                {
                    frame.X = -menuWidth;
                }
                else if (m_FlyMode == FlyoutMode.Top)
                    frame.Y = menuHeight;
		    }

		    setViewSize ();
			SetLocation (frame);

		    if (this.TargetViewController != null)
		    {
                this.TargetViewController.View.AddSubview(mainView);
                this.TargetViewController.AddChildViewController(CurrentViewController);
		    }
            else
		    {
		        this.View.AddSubview(mainView);
		        this.AddChildViewController(CurrentViewController);
		    }

		    if (!HideShadow)
				this.View.InsertSubviewBelow (shadowView, mainView);
			if (!ShouldStayOpen)
				HideMenu ();
			if (SelectedIndexChanged != null)
				SelectedIndexChanged ();
			
		}
		
		//bool isOpen {get{ return mainView.Frame.X == menuWidth; }}

		public bool IsOpen {
			get
			{
			    if (m_FlyMode == FlyoutMode.Top)
			        return Math.Abs(mainView.Frame.Y - menuHeight) < float.Epsilon;

			    return Math.Abs(mainView.Frame.X - menuWidth) < float.Epsilon;
			}
			set { 
				if (value)
					HideMenu ();
				else
					ShowMenu ();
			}
		}
		
		public void ShowMenu ()
		{
//			if (isOpen)
//				return;
			EnsureInvokedOnMainThread (delegate {
				//navigation.ReloadData ();
				//isOpen = true;
				closeButton.Frame = mainView.Frame;
				shadowView.Frame = mainView.Frame;
//				if (!HideShadow)
//					this.View.InsertSubviewBelow (shadowView, mainView);
				if (!ShouldStayOpen)
					this.View.AddSubview (closeButton);
				UIView.BeginAnimations ("slideMenu");
				UIView.SetAnimationCurve (UIViewAnimationCurve.EaseIn);
				//UIView.SetAnimationDuration(2);
				setViewSize ();
				var frame = mainView.Frame;

                if (m_FlyMode == FlyoutMode.Left)
                    frame.X = menuWidth;
                else if (m_FlyMode == FlyoutMode.Right)
                {
                    frame.X = -menuWidth;
                }
                else if (m_FlyMode == FlyoutMode.Top)
                    frame.Y = menuHeight;

				SetLocation (frame);
				setViewSize ();
				frame = mainView.Frame;
				shadowView.Frame = frame;
				closeButton.Frame = frame;
				UIView.CommitAnimations ();
			});
		}

		bool ShouldStayOpen {
			get {
				if (ForceMenuOpen || (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad && 
					AlwaysShowLandscapeMenu && 
					(this.InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft 
					|| this.InterfaceOrientation == UIInterfaceOrientation.LandscapeRight)))
					return true;
				return false;	
			}
		}

		private void setViewSize ()
		{
			var frame = View.Bounds;
			//frame.Location = PointF.Empty;
		    if (ShouldStayOpen)
		    {
                if (m_FlyMode == FlyoutMode.Left)
                    frame.Width -= menuWidth;
                else if (m_FlyMode == FlyoutMode.Right)
                {
                    frame.Width += menuWidth;
                }
                else if (m_FlyMode == FlyoutMode.Top)
                    frame.Height -= menuHeight;
		        
		    }

		    if (mainView.Bounds == frame)
				return;

			mainView.Bounds = frame;
		}

		private void SetLocation (RectangleF frame)
		{
			
			mainView.Layer.AnchorPoint = new PointF(.5f, .5f);
            if (m_FlyMode == FlyoutMode.Left)
                frame.Y = 0;
            else if (m_FlyMode == FlyoutMode.Right)
            {
                frame.Y = 0;
            }
            else if (m_FlyMode == FlyoutMode.Top)
                frame.X = 0;

			if (mainView.Frame.Location == frame.Location)
				return;
			frame.Size = mainView.Frame.Size;
			var center = new PointF (frame.Left + frame.Width / 2,
			                        frame.Top + frame.Height / 2);
			mainView.Center = center;
			shadowView.Center = center;
		}
		
		public void HideMenu ()
		{
//			if (!IsOpen)
//				return;
			EnsureInvokedOnMainThread (delegate {

				//isOpen = false;
				navigation.FinishSearch ();
				closeButton.RemoveFromSuperview ();
				shadowView.Frame = mainView.Frame;
				//UIView.AnimationWillEnd += hideComplete;
				UIView.BeginAnimations ("slideMenu");
				UIView.SetAnimationDidStopSelector (new Selector ("animationEnded"));
				//UIView.SetAnimationDuration(.5);
				UIView.SetAnimationCurve (UIViewAnimationCurve.EaseInOut);
				var frame = this.View.Bounds;
                if (m_FlyMode == FlyoutMode.Left)
                    frame.X = 0;
                else if (m_FlyMode == FlyoutMode.Right)
                {
                    frame.X = 0;
                }
                else if (m_FlyMode == FlyoutMode.Top)
                    frame.Y = 0;

				setViewSize ();
				SetLocation (frame);
				shadowView.Frame = frame;
				UIView.CommitAnimations ();
			});
		}

		public UIColor BackgroundColor { get { return navigation.TableView.BackgroundColor; } set { navigation.TableView.BackgroundColor = value;}}

		[Export("animationEnded")]
		private void hideComplete ()
		{
			shadowView.RemoveFromSuperview ();
		}

		public void ResignFirstResponders (UIView view)
		{
		    if (view == null)
		        return;

			if (view.Subviews == null)
				return;
			foreach (var subview in view.Subviews) {
				if (subview.IsFirstResponder)
					subview.ResignFirstResponder ();
				ResignFirstResponders (subview);
			}
		}
		
		public void ToggleMenu ()
		{
			EnsureInvokedOnMainThread (delegate {
				if (!IsOpen && CurrentViewController != null && CurrentViewController.IsViewLoaded)
					ResignFirstResponders (CurrentViewController.View);
				if (IsOpen)
					HideMenu ();
				else
					ShowMenu ();
			});
		}

		private int selectedIndex;

		public int SelectedIndex {
			get{ return selectedIndex;}
			set {
				if (selectedIndex == value)
					return;
				selectedIndex = value;
				EnsureInvokedOnMainThread (delegate {
					NavigationItemSelected (value);
				});
			}
		}
		
		private int GetIndex (NSIndexPath indexPath)
		{
			int section = 0;
			int rowCount = 0;
			while (section < indexPath.Section) {
				rowCount += navigation.Root [section].Count;
				section ++;
			}
			return rowCount + indexPath.Row;
		}

		protected NSIndexPath GetIndexPath (int index)
		{
			if (navigation.Root == null)
				return NSIndexPath.FromRowSection (0, 0);
			int currentCount = 0;
			int section = 0;
			foreach (var element in navigation.Root) {
				if (element.Count + currentCount > index)
					break;
				currentCount += element.Count;
				section ++;
			}
			
			var row = index - currentCount;
			return NSIndexPath.FromRowSection (row, section);
		}

		public bool DisableRotation { get; set; }

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			if (DisableRotation)
				return toInterfaceOrientation == InterfaceOrientation;
			
			var theReturn = CurrentViewController == null ? true : CurrentViewController.ShouldAutorotateToInterfaceOrientation (toInterfaceOrientation);
			return theReturn;
		}
		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations ()
		{
			if(CurrentViewController != null)
				return CurrentViewController.GetSupportedInterfaceOrientations();
			return UIInterfaceOrientationMask.All;
		}
		public override bool ShouldAutomaticallyForwardRotationMethods {
			get {
				return true;
			}
		}
		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration)
		{
			base.WillRotate (toInterfaceOrientation, duration);
		}

		public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation)
		{
			base.DidRotate (fromInterfaceOrientation);

		    FlyMode = m_FlyMode;//re-init sizes.

            //hide if we are phone and rotated as leaving it open could effect top view
		    if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                HideMenu();
		        return;
		    }
		    switch (InterfaceOrientation) {
			case UIInterfaceOrientation.LandscapeLeft:
			case UIInterfaceOrientation.LandscapeRight:
				ShowMenu ();
				return;
			default:
				HideMenu ();
				return;
			}
			setViewSize ();
			
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration)
		{
			base.WillAnimateRotation (toInterfaceOrientation, duration);
		}

		protected void EnsureInvokedOnMainThread (Action action)
		{
			if (IsMainThread ()) {
				action ();
				return;
			}
			this.BeginInvokeOnMainThread (() => 
			                              action ()
			);
		}

		private static bool IsMainThread ()
		{
			return NSThread.Current.IsMainThread;
			//return Messaging.bool_objc_msgSend(GetClassHandle("NSThread"), new Selector("isMainThread").Handle);
		}

	}
}

