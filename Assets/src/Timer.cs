using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEditor;

public class Timer : MonoBehaviour
{
    enum TimerType { Counter, CircularFitness }
    enum State { Prepare, Fitness, Rest, Pause }
    enum ButtonType { Start, Control, Reset, Switch, Setting, SwitchAudio, Quit, End }
    const float INTERVAR = 0.001f;
    const int
        DFT_FITNESS = 45000, DFT_REST = 15000;
    const string 
        FITNESS_KEY = "fitness", REST_KEY = "rest", AUDIOGROUP_KEY = "audio", TIMERTYPE_KEY = "timer";
    static Color[] colors = new Color[]
    {
        new Color(0f, 1f, 0f), // green
        new Color(1f, 0f, 0f), // red
    };

    
    [SerializeField] Image timerImg;
    [SerializeField] Text timerText;
    [SerializeField] Text completeText;
    [SerializeField] Transform btnParent;
    [SerializeField] InputField inputFitness;
    [SerializeField] InputField inputRest;
    [SerializeField] AudioSource audioSrc;
    [SerializeField] AudioClip startClip;
    [SerializeField] List<AudioClip> audios;

    Dictionary<ButtonType, Text> btnTexts = new Dictionary<ButtonType, Text>();

    [SerializeField] List<List<AudioClip>> audioClips = new List<List<AudioClip>>();
    Color color;

    int 
        fitnessTime, restTime, timer, completeCount, audioGroupIndex;
    
    State state;
    State lastCircularFitnessState;
    TimerType timerType;

    float updateTimer;
    bool updateTimerEnable;

    void Awake() 
    {
        state = State.Prepare;
        updateTimerEnable = true;
        int slice = audios.Count / 10;
        int reamin = audios.Count % 10;
        int i = -1;
        while (++i < slice)
        {
            audioClips.Add(audios.GetRange(i * 10, 10));
        }
        if (reamin > 0)
        {
            audioClips.Add(audios.GetRange(slice * 10, reamin));
        }
        Button prefab = Resources.Load<Button>("btn");
        ButtonType btnType = ButtonType.Start;
        while (++btnType < ButtonType.End)
        {
            Button btn = Instantiate(prefab, btnParent);
            Text text = btn.GetComponentInChildren<Text>();
            text.text = GetButtonTitle(btnType);
            btnTexts.Add(btnType, text);
            btn.onClick.AddListener(GetButtonAction(btnType));
        }
        fitnessTime = PlayerPrefs.HasKey(FITNESS_KEY) ? PlayerPrefs.GetInt(FITNESS_KEY) : DFT_FITNESS;
        restTime = PlayerPrefs.HasKey(REST_KEY) ? PlayerPrefs.GetInt(REST_KEY) : DFT_REST;
        audioGroupIndex = PlayerPrefs.HasKey(AUDIOGROUP_KEY) ? PlayerPrefs.GetInt(AUDIOGROUP_KEY) : 0;
        timerType = PlayerPrefs.HasKey(TIMERTYPE_KEY) ? (TimerType)PlayerPrefs.GetInt(TIMERTYPE_KEY) : TimerType.CircularFitness;
        inputFitness.onEndEdit.AddListener((content) => 
        {
            fitnessTime = int.Parse(content);
        });
        inputRest.onEndEdit.AddListener((content) => 
        {
            restTime = int.Parse(content);
        });
        ResetAction();
    }

    void Update()
    {
        if (!updateTimerEnable || state == State.Prepare || state == State.Pause) { return; }

        updateTimer += Time.deltaTime;
        while (updateTimer >= INTERVAR)
        {
            updateTimer -= INTERVAR;
            switch (timerType)
            {
                case TimerType.Counter:
                    {
                        if (timer == 0)
                        {
                            PlayStartAudio();
                        }
                        timer += 1;
                    }
                    break;
                case TimerType.CircularFitness:
                    {
                        timer -= 1;
                        if (timer == 0)
                        {
                            audioSrc.PlayOneShot(startClip);
                            if (state == State.Rest)
                            {
                                ++completeCount;
                                UpdateCompleteText();
                            }
                            state = state == State.Fitness ? State.Rest : State.Fitness;
                            ResetTimer();
                        }
                        else if (timer <= 10000)
                        {
                            if ((timer + 1000) % 1000 == 0)
                            {
                                PlayCounterAudio(timer / 1000 - 1);
                            }
                        }
                        else if (timer <= 11000)
                        {
                            timerImg.color = Color.Lerp(colors[0], colors[1], 1f - (timer - 10000) / 1000f);
                        }
                        UpdateFillAmount(state == State.Fitness ? (float)timer / fitnessTime : (float)timer / restTime);
                    }
                    break;
            }
            UpdateTimerText();
        }
    }

    void UpdateTimerText()
    {
        timerText.text = (timer / 1000f).ToString("0.0");
    }

    void UpdateCompleteText()
    {
        completeText.text = string.Format("已完成：<color=#>{0}次</color>", completeCount);
    }

    void UpdateControlText()
    {
        btnTexts[ButtonType.Control].text = GetButtonTitle(ButtonType.Control);
    }
    
    void UpdateSwitchText()
    {
        btnTexts[ButtonType.Switch].text = GetButtonTitle(ButtonType.Switch);
    }

    void ResetTimer()
    {
        timer = timerType == TimerType.Counter ? 0 
            : state == State.Rest ? restTime : fitnessTime;

        UpdateFillAmount(1f);
        timerImg.color = colors[0];
    }

    void PlayStartAudio()
    {
        if (startClip != null)
        {
            audioSrc.PlayOneShot(startClip);
        }
    }

    void PlayCounterAudio(int index)
    {
        if (audioClips != null && audioClips.Count > 0 && index >= 0 && index < audioClips[audioGroupIndex].Count)
        {
            audioSrc.PlayOneShot(audioClips[audioGroupIndex][index]);
        }
    }

    void UpdateFillAmount(float value)
    {
        timerImg.fillAmount = timerType == TimerType.CircularFitness ? value : 1f;
    }

    UnityAction GetButtonAction(ButtonType btnType)
    {
        UnityAction action  = () => { Debug.Log("Nothing attaced."); };
        switch (btnType)
        {
            case ButtonType.Control:
                action = ControlAction;
                break;
            case ButtonType.Reset:
                action = ResetAction;
                break;
            case ButtonType.Switch:
                action = SwitchAction;
                break;
            case ButtonType.SwitchAudio:
                action = SwitchAudioAction;
                break;
            case ButtonType.Setting:
                action = SettingAction;
                break;
            case ButtonType.Quit:
                action = QuitAction;
                break;
        }
        return action;
    }

    string GetButtonTitle(ButtonType btnType)
    {
        string title = string.Empty;
        switch (btnType)
        {
            case ButtonType.Control:
                switch (state)
                {
                    case State.Prepare:
                        title = "开始";
                        break;
                    case State.Fitness:
                    case State.Rest:
                        title = "暂停";
                        break;
                    case State.Pause:
                        title = "继续";
                        break;
                }
                
                break;
            case ButtonType.Switch:
                title = timerType == TimerType.Counter ? "切换到组计时模式" : "切换到计时器模式";
                break;
            case ButtonType.SwitchAudio:
                title = "下一组音频";
                break;
            case ButtonType.Reset:
                title = "重置";
                break;
            case ButtonType.Setting:
                title = "设置";
                break;
            case ButtonType.Quit:
                title = "退出";
                break;
        }
        return title;
    }

    void ControlAction()
    {
        switch (state)
        {
            case State.Prepare:
                state = State.Fitness;
                break;
            case State.Fitness:
            case State.Rest:
                lastCircularFitnessState = state;
                state = State.Pause;
                break;
            case State.Pause:
                state = lastCircularFitnessState;
                break;
        }
        UpdateControlText();
        UpdateTimerText();
    }

    void SwitchAction()
    {
        timerType = timerType == TimerType.Counter ? TimerType.CircularFitness : TimerType.Counter;
        ResetAction();
        UpdateSwitchText();
        completeText.gameObject.SetActive(timerType == TimerType.CircularFitness);
    }

    void SwitchAudioAction()
    {
        audioGroupIndex = ++audioGroupIndex == audioClips.Count ? 0 : audioGroupIndex;
        PlayerPrefs.SetInt(AUDIOGROUP_KEY, audioGroupIndex);
    }

    void ResetAction()
    {
        state = State.Prepare;
        ResetTimer();
        UpdateControlText();
        UpdateTimerText();
    }

    void SettingAction()
    {
        updateTimerEnable = false;
        inputFitness.transform.parent.gameObject.SetActive(true);
    }

    void QuitAction()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SaveTime()
    {
        PlayerPrefs.SetInt(FITNESS_KEY, fitnessTime);
        PlayerPrefs.SetInt(REST_KEY, restTime);
        inputFitness.transform.parent.gameObject.SetActive(false);
        updateTimerEnable = true;
    }
}
