using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.LuminousVector.Karuta
{
	class Word
	{
		public float noun
		{
			set { _noun = Utils.Clamp(value, RANGE); }
			get { return _noun; }
		}
		public float adj
		{
			set { _adj = Utils.Clamp(value, RANGE); }
			get { return _adj; }
		}
		public float verb
		{
			set { _verb = Utils.Clamp(value, RANGE); }
			get { return _verb; }
		}
		public float pNoun
		{
			set { _pNoun = Utils.Clamp(value, RANGE); }
			get { return _pNoun; }
		}
		public float aVerb
		{
			set { _aVerb = Utils.Clamp(value, RANGE); }
			get { return _aVerb; }
		}

		private readonly float RANGE = 10;
		private float _noun, _adj, _verb, _pNoun, _aVerb;

		public Word(string value)
		{

		}
	}
}
