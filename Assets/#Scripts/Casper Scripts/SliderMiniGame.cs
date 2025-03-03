using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderMiniGame : MonoBehaviour
{
    [SerializeField] private List<Slider> _sliders = new List<Slider>();

    private List<float> _sliderSolutions = new List<float>();

    private void Awake()
    {
        InitSliderSolutions();

        foreach (Slider slider in _sliders)
        {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    private void OnSliderValueChanged(float arg0)
    {
        
    }

    private void Update()
    {
        AllignSliders();
    }

    private void InitSliderSolutions()
    {
        int index = 0;

        foreach (var slider in _sliders)
        {
            float solVal = Random.Range(slider.minValue, slider.maxValue);
            _sliderSolutions.Insert(index, solVal);
            index++;
        }
    }

    private void AllignSliders()
    {
        
    }
}
