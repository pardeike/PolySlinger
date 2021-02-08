using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class SettingsController<T> : MonoBehaviour where T : MonoBehaviour
{
	public bool saveSettings = false;

	static T _settings;
	public static T BaseInstance
	{
		get
		{
			if (_settings == null)
			{
				var obj = GameObject.Find("GameSettings");
				if (obj == null)
				{
					obj = new GameObject("GameSettings");
					_ = obj.AddComponent<T>();
				}
				_settings = obj.GetComponent<T>();
			}
			return _settings;
		}
	}

	static FieldInfo[] _fields;
	FieldInfo[] AllFields
	{
		get
		{
			if (_fields == null)
				_fields = GetType().GetFields()
					.Where(field => field.Name != "saveSettings")
					.OrderBy(field => field.MetadataToken).ToArray();
			return _fields;
		}
	}

	public int Count => AllFields.Length;

	public string Name(int idx)
	{
		var names = AllFields.Select(field => field.Name).ToArray();
		return names[idx];
	}

	public (float, float) GetFloatRange(int idx)
	{
		var field = GetType().GetField(Name(idx));
		var interval = field.GetCustomAttributes(typeof(SettingsIntervalAttribute), false)
			.Cast<SettingsIntervalAttribute>().First();
		return (interval.min, interval.max);
	}

	public float GetFloatValue(int idx)
	{
		return (float)GetType().GetField(Name(idx)).GetValue(this);
	}

	public void SetFloatValue(int idx, float val)
	{
		GetType().GetField(Name(idx)).SetValue(this, val);
	}

	public void Save()
	{
		foreach (var field in AllFields)
		{
			var type = field.FieldType;
			var name = field.Name;
			setters[type](name, field.GetValue(this));
		}
	}

	static readonly Dictionary<Type, Func<string, object>> getters = new Dictionary<Type, Func<string, object>> {
		{ typeof(float), (key) => PlayerPrefs.GetFloat(key) },
		{ typeof(int), (key) => PlayerPrefs.GetInt(key) },
		{ typeof(string), (key) => PlayerPrefs.GetString(key) },
	};

	static readonly Dictionary<Type, Action<string, object>> setters = new Dictionary<Type, Action<string, object>> {
		{ typeof(float), (key, val) => PlayerPrefs.SetFloat(key, (float)val) },
		{ typeof(int), (key, val) => PlayerPrefs.SetInt(key, (int)val) },
		{ typeof(string), (key, val) => PlayerPrefs.SetString(key, (string)val) },
	};

	void Awake()
	{
		DontDestroyOnLoad(gameObject);

		foreach (var field in AllFields)
		{
			var type = field.FieldType;
			var name = field.Name;
			if (PlayerPrefs.HasKey(name))
				field.SetValue(this, getters[type](name));
			else
				setters[type](name, field.GetValue(this));
		}
	}

	void OnValidate()
	{
		if (saveSettings)
		{
			saveSettings = false;
			Save();
			PlayerPrefs.Save();
		}
	}

	void OnApplicationQuit()
	{
		Save();
	}
}
