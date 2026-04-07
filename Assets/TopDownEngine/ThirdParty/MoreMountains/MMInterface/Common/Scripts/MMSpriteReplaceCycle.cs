using UnityEngine;
using System.Collections;
using System;
using MoreMountains.Tools;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace MoreMountains.MMInterface
{	
	/// <summary>
	/// A class to add to a button so that its sprite cycles through X sprites when pressed
	/// </summary>
	public class MMSpriteReplaceCycle : MonoBehaviour 
	{
		/// the list of sprites to cycle through
		public Sprite[] Sprites;
		/// the sprite index to start on
		public int StartIndex = 0;
		/// when calling Animate (ideally at Update), the framerate to apply (number of frames per second)
		public int FrameRate = 10;
		
		protected SpriteRenderer _spriteRenderer;
		protected Image _image;
		protected MMTouchButton _mmTouchButton;
		protected int _currentIndex = 0;
		
		protected bool _imageIsNull = false;
		protected bool _spriteRendererIsNull = false;
		protected bool _initialized = false;

		protected float _timeSinceLastFrame = 0f;
		protected float _timePerFrame = 0f;

		/// <summary>
		/// On Start we initialize our cycler
		/// </summary>
		protected virtual void Start()
		{
			if (!_initialized)
			{
				Initialization ();	
			}
		}

		/// <summary>
		/// On init, we grab our image component, and set our first sprite as specified
		/// </summary>
		protected virtual void Initialization()
		{
			_initialized = true;
			_mmTouchButton = GetComponent<MMTouchButton> ();
			if (_mmTouchButton != null)
			{
				_mmTouchButton.ReturnToInitialSpriteAutomatically = false;
			}
			_spriteRenderer = GetComponent<SpriteRenderer> ();
			_image = GetComponent<Image> ();
			_imageIsNull = (_image == null);
			_spriteRendererIsNull = (_spriteRenderer == null);
			_timePerFrame = 1f / FrameRate;
			
			SwitchToIndex(StartIndex);
		}

		/// <summary>
		/// Changes the image's sprite to the next sprite in the list
		/// </summary>
		public virtual void Swap()
		{
			_currentIndex++;
			if (_currentIndex >= Sprites.Length)
			{
				_currentIndex = 0;
			}
			{
				SwitchToIndex (_currentIndex);
			}
		}

		public virtual void SwitchToRandom()
		{
			SwitchToIndex(Random.Range(0, Sprites.Length));	
		}

		/// <summary>
		/// A public method to set the sprite directly to the one specified in parameters
		/// </summary>
		/// <param name="index">Index.</param>
		public virtual void SwitchToIndex(int index)
		{
			if (!_initialized)
			{
				Initialization ();	
			}
			if (Sprites.Length <= index) { return; }
			if (Sprites[index] == null) { return; }
			
			if (!_imageIsNull)
			{
				_image.sprite = Sprites[index];	
			}
			if (!_spriteRendererIsNull)
			{
				_spriteRenderer.sprite = Sprites[index];
			}
			_currentIndex = index;
		}

		public virtual void Animate()
		{
			_timeSinceLastFrame += Time.deltaTime;

			if (_timeSinceLastFrame >= _timePerFrame)
			{
				_timeSinceLastFrame -= _timePerFrame;
				Swap();
			}
		}
	}
}