﻿using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace Microsoft.Maui.Platform
{
	// TODO ezhart At this point, this is almost exactly a clone of LayoutViewGroup; we may be able to drop this class entirely
	public class ContentViewGroup : MauiViewGroup
	{
		IBorderStroke? _clip;
		readonly Context _context;

		public ContentViewGroup(Context context) : base(context)
		{
			_context = context;
		}

		public ContentViewGroup(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			var context = Context;
			ArgumentNullException.ThrowIfNull(context);
			_context = context;
		}

		public ContentViewGroup(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			_context = context;
		}

		public ContentViewGroup(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
		{
			_context = context;
		}

		public ContentViewGroup(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
		{
			_context = context;
		}

		protected override void DispatchDraw(Canvas? canvas)
		{
			if (Clip != null)
				ClipChild(canvas);

			base.DispatchDraw(canvas);
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (CrossPlatformMeasure == null)
			{
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
				return;
			}

			var deviceIndependentWidth = widthMeasureSpec.ToDouble(_context);
			var deviceIndependentHeight = heightMeasureSpec.ToDouble(_context);

			var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
			var heightMode = MeasureSpec.GetMode(heightMeasureSpec);

			var measure = CrossPlatformMeasure(deviceIndependentWidth, deviceIndependentHeight);

			// If the measure spec was exact, we should return the explicit size value, even if the content
			// measure came out to a different size
			var width = widthMode == MeasureSpecMode.Exactly ? deviceIndependentWidth : measure.Width;
			var height = heightMode == MeasureSpecMode.Exactly ? deviceIndependentHeight : measure.Height;

			var platformWidth = _context.ToPixels(width);
			var platformHeight = _context.ToPixels(height);

			// Minimum values win over everything
			platformWidth = Math.Max(MinimumWidth, platformWidth);
			platformHeight = Math.Max(MinimumHeight, platformHeight);

			SetMeasuredDimension((int)platformWidth, (int)platformHeight);
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			if (CrossPlatformArrange == null)
			{
				return;
			}

			var destination = _context.ToCrossPlatformRectInReferenceFrame(left, top, right, bottom);

			CrossPlatformArrange(destination);
		}

		internal IBorderStroke? Clip
		{
			get => _clip;
			set
			{
				_clip = value;
				PostInvalidate();
			}
		}

		internal Func<double, double, Graphics.Size>? CrossPlatformMeasure { get; set; }
		internal Func<Graphics.Rect, Graphics.Size>? CrossPlatformArrange { get; set; }

		void ClipChild(Canvas? canvas)
		{
			if (Clip is null || Clip?.Shape is null || canvas is null)
				return;

			float density = _context.GetDisplayDensity();

			float strokeThickness = (float)(Clip.StrokeThickness * density);

			IShape clipShape = Clip.Shape;

			float x = strokeThickness / 2;
			float y = strokeThickness / 2;
			float w = canvas.Width - strokeThickness;
			float h = canvas.Height - strokeThickness;

			var bounds = new Graphics.RectF(x, y, w, h);

			Path? platformPath = clipShape.ToPlatform(bounds, strokeThickness, true);

			if (platformPath is not null)
				canvas.ClipPath(platformPath);
		}
	}
}