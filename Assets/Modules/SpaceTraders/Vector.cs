using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Vector<T> {
	private T[] _data;

	private int _maxLength = 0;

	private int _length = 0;
	public int length {
		get { return _length; }
	}

	public Vector(T value) : this(new T[1] { value }) { }

	public Vector(IEnumerable<T> values = null) {
		_maxLength = values == null ? 2 : Mathf.Min(1, values.Count() * 2);
		_data = new T[_maxLength];
		if (values != null) foreach (T element in values) Append(element);
	}

	public void Append(T value) {
		if (_maxLength == length) {
			_maxLength *= 2;
			T[] newData = new T[_maxLength];
			for (int i = 0; i < length; i++) newData[i] = _data[i];
			_data = newData;
		}
		_data[_length++] = value;
	}

	public T RemoveRandom() {
		if (length == 0) throw new UnityException("Vector is empty");
		int index = Random.Range(0, length);
		T result = _data[index];
		_length -= 1;
		if (index != _length) _data[index] = _data[length];
		return result;
	}
}
