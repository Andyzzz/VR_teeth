
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using NetMQ;
using UnityEngine;
using NetMQ.Sockets;
using UnityEngine.UI;

public class NetMqListener
{
    private readonly Thread _listenerWorker2;

    private bool _listenerCancelled2;

    public delegate void MessageDelegate(string message);

    private readonly MessageDelegate _messageDelegate2;

    private readonly ConcurrentQueue<string> _messageQueue2 = new ConcurrentQueue<string>();

    private void ListenerWork2()
    {
        AsyncIO.ForceDotNet.Force();
        using (var subSocket = new SubscriberSocket())
        {
            subSocket.Options.ReceiveHighWatermark = 1000;
            subSocket.Connect("tcp://localhost:12345");
            subSocket.Subscribe("");
            while (!_listenerCancelled2)
            {
                string frameString;
                if (!subSocket.TryReceiveFrameString(out frameString)) continue;
                //Debug.Log(frameString);
                _messageQueue2.Enqueue(frameString);
            }
            subSocket.Close();
        }
        NetMQConfig.Cleanup();
    }

    public void Update()
    {
        while (!_messageQueue2.IsEmpty)
        {
            string message;
            if (_messageQueue2.TryDequeue(out message))
            {
                _messageDelegate2(message);
            }
            else
            {
                break;
            }
        }
    }

    public NetMqListener(MessageDelegate messageDelegate)
    {
        _messageDelegate2 = messageDelegate;
        _listenerWorker2 = new Thread(ListenerWork2);
    }

    public void Start()
    {
        _listenerCancelled2 = false;
        _listenerWorker2.Start();
    }

    public void Stop()
    {
        _listenerCancelled2 = true;
        _listenerWorker2.Join();
    }
}


public class newcameraClient2 : MonoBehaviour
{
    private NetMqListener _netMqListener;
    public InputField InputUL;    // choose up or low
    public InputField InputText;  // choose tooth number
    public InputField InputText1; // vertical angle input
    public InputField InputText2; // horizontal angle input
    GameObject input;
    public string txt;  // tooth number string
    public Text Text2;  // show the current tooth number
    public Text TextMode;
    float x = 0f;  // vertical angle
    float y = 0f; // horizontal angle 

    string UL = "L";
    int flg = 1;  // current mode flag, A: tracker input, B: mannual input
    int num = -1; // tooth num

    // camera location dictionary of Upper teeth==================================================================
    public Dictionary<int, float> camLocX_U = new Dictionary<int, float> {
            {-1,  -164.04f},{-2, -164.79f},{-3, -175.31f},{-4, -186.64f},{-5, -187.76f},{-6, -200.04f},{-7, -215.21f},
            {1,  -163.3f},{2, -162.5f},{3, -153.2f},{4, -141.1f},{5, -141.28f},{6, -127.94f},{7, -113.01f}
        };
    public Dictionary<int, float> camLocY_U = new Dictionary<int, float> {
            {-1,  -0.14f}, {-2, -0.14f}, {-3, -1.52f}, {-4, -0.6f},{-5, -0.6f},{-6, -0.6f},{-7, -0.6f},
            {1,  -0.27f}, {2, -0.27f}, {3, -0.27f}, {4, -0.27f},{5, -0.27f},{6, -0.27f},{7, -0.27f}
        };
    public Dictionary<int, float> camLocZ_U = new Dictionary<int, float> {
            {-1,  2.22f}, {-2, 2.22f}, {-3, 1.45f}, {-4, 1.45f}, {-5, 1.45f},{-6, 1.45f},{-7, 1.45f},
            {1,  1.96f}, {2, 1.96f}, {3, 1.96f}, {4, 1.96f}, {5, 1.96f},{6, 1.96f},{7, 1.96f}
        };

    //camera rotation dictionary of Upper teeth
    public Dictionary<int, float> camRotX_U = new Dictionary<int, float> {
            {-1, 60f}, {-2, 60f}, {-3, 75f},{-4,57f},{-5,57f},{-6,54f},{-7,50f},
            {1, 60f}, {2, 60f}, {3, 75f},{4,57f},{5,57f},{6,54f},{7,50f}
        };
    //==============================================================================================================

    // camera location dictionary of Lower teeth
    public Dictionary<int, float> camLocX_L = new Dictionary<int, float> {
            {-1,  -0.29f},{-2, -0.8f},{-3, -14.3f},{-4, -28.5f},{-5, -29.2f},{-6, -43.8f},{-7, -45.1f},
            {1,  0.4f},{2, 1.0f},{3, 13.7f},{4, 23.98f},{5, 24.89f},{6, 38.5f},{7, 39.87f}
        };
    public Dictionary<int, float> camLocY_L = new Dictionary<int, float> {
            {-1,  0.5f}, {-2, 0.5f}, {-3, 0.1f}, {-4, -0.4f},{-5, -0.4f},{-6, -0.4f},{-7, -0.4f},
            {1,  0.5f}, {2, 0.5f}, {3, 0.2f}, {4, -0.2f},{5, -0.2f},{6, -0.15f},{7, -0.15f}
        };
    public Dictionary<int, float> camLocZ_L = new Dictionary<int, float> {
            {-1,  0.7f}, {-2, 0.7f}, {-3, 0.8f}, {-4, 1.1f}, {-5, 1.1f},{-6, 1.1f},{-7, 1.1f},
            {1,  0.9f}, {2, 0.9f}, {3, 1.0f}, {4, 1.4f}, {5, 1.4f},{6, 0.9f},{7, 0.9f}
        };

    //camera rotation dictionary of Lower teeth
    public Dictionary<int, float> camRotX_L = new Dictionary<int, float> {
            {-1, -7f}, {-2, -7f}, {-3, -18f},{-4,-26f},{-5,-26f},{-6,-20f},{-7,-20f},
            {1, -7f}, {2, -7f}, {3, -18f},{4,-26f},{5,-26f},{6,-20f},{7,-20f}
        };

    //tooth delta ry
    public Dictionary<int, float> toothRy = new Dictionary<int, float> {
            {-1, 0f}, {-2, 0f}, {-3, -45f},{-4,-72f},{-5,-72f},{-6,-78f},{-7,-78f},
            {1, 0f}, {2, 0f}, {3, 45f},{4,72f},{5,72f},{6,78f},{7,78f}
        };
    private void HandleMessage(string message)
    {
        var splittedStrings = message.Split(' ');
        //Debug.Log(splittedStrings.Length);
        Debug.Log(message);
        //if (splittedStrings.Length != 6) return;
        var x = float.Parse(splittedStrings[0]);
        var y = float.Parse(splittedStrings[1]);
        var z = float.Parse(splittedStrings[2]);
        var rx = float.Parse(splittedStrings[3]);
        var ry = float.Parse(splittedStrings[4]);
        var rz = float.Parse(splittedStrings[5]);

        //if (rx > 0)
        //{
        //    rx = 90 - rx;
        //}
        //else
        //{
        //    rx = -rx - 90;
        //}

        try
        {
            // getting parsed value 
            num = int.Parse(txt);
        }

        catch(FormatException)
        {
            //Debug.Log(txt);
            num = -1;
        } 
        ry = ry - 180;
        if (ry > 0)
        {
            ry = ry - System.Math.Abs(toothRy[num]);
        }
        else
        {
            ry = System.Math.Abs(toothRy[num]) - System.Math.Abs(ry);
        }

        changeLight(-rx-90, -ry);

    }
    public void HandleMessage2(float x, float y)
    {
        var rx = x;
        var ry = y;
        //int num = int.Parse(txt);
        try
        {
            // getting parsed value 
            num = int.Parse(txt);
        }

        catch (FormatException)
        {
            //Debug.Log(txt);
            num = -1;
        }
        changeLight(rx, ry);
        //Debug.Log(rx);

    }
    void GetEnd(string value)
    {
        try
        {
            txt = value;
            // getting parsed value 
            num = int.Parse(value);
        }
        catch (FormatException)
        {
            Text2.text = "Your input is invalid, please input again!";
            //Debug.Log(value);
            //num = -1;
        }

        if (System.Math.Abs(num) < 1 || System.Math.Abs(num) > 7)
        {
            Text2.text = "Your input is invalid, please input again!";
            //num = -1;
        }
        else
        {
            txt = value;
            num = int.Parse(value);
            Text2.text = " The current tooth number is: " + UL + ", " + txt;
        }

        
        //Debug.Log("End ");
    }
    void GetEnd1(string value)
    {
        x = float.Parse(value); // vertical
    }
    void GetEnd2(string value)
    {
        y = float.Parse(value); // horizontal

    }
    void GetEnd3(string value)
    {
        if (!(value == "U" || value == "u" || value == "L" || value == "l"))
        {
            Text2.text = "Your input is invalid, please input again";
            //UL = "L";
        }
        else
        {
            UL = value;
            Text2.text = "The current tooth number is: " + UL + ", " + txt;
        }
    }
    void changeCamera()
    {
        try
        {
            // getting parsed value 
            num = int.Parse(txt);
        }

        catch (FormatException)
        {
            //Text2.text = "2 Your input is  invalid, please input again!";
            //Debug.Log(txt);
            num = -1;
        }
        if (System.Math.Abs(num) < 1 || System.Math.Abs(num) > 7)
        {
            Text2.text = "Your input is invalid, please input again!";
            num = -1;
        }
        if (!(UL == "U" || UL == "u" || UL == "L" || UL == "l"))
        {
            Text2.text = "Your input is  invalid, please input again!";
            UL = "L";
        }
        if (UL == "U" || UL == "u")
        {
            transform.position = new Vector3(camLocX_U[num], camLocY_U[num], camLocZ_U[num]);
            transform.localEulerAngles = new Vector3(camRotX_U[num], 0f, 0f);
        }
        if (UL == "L" || UL == "l")
        {
            transform.position = new Vector3(camLocX_L[num], camLocY_L[num], camLocZ_L[num]);
            transform.localEulerAngles = new Vector3(camRotX_L[num], 0f, 0f);
        }
        //transform.position = new Vector3(camLocX_L[num], camLocY_L[num], camLocZ_L[num]);
        //transform.localEulerAngles = new Vector3(camRotX_L[num], 0f, 0f);
    }
    void changeLight(float rx, float ry)
    {
        GameObject lightobj = GameObject.Find("Directional Light");
        lightobj.transform.localEulerAngles = new Vector3(rx, ry, 0f); //ry需要减去牙齿本身的水平角
        //Debug.Log(lightobj.transform.localEulerAngles);
    }
    public void Start()
    {
        
        _netMqListener = new NetMqListener(HandleMessage);
        _netMqListener.Start();

        input = GameObject.Find("InputField");
        InputText = input.GetComponent<InputField>();
        InputText.onEndEdit.AddListener(GetEnd);

        Text2 = GameObject.Find("Text2").GetComponent<Text>();
        TextMode = GameObject.Find("Text_mode").GetComponent<Text>();            // tooth number
        
        InputText1 = GameObject.Find("InputField1").GetComponent<InputField>();  // vertical angle
        InputText1.onEndEdit.AddListener(GetEnd1);

        InputText2 = GameObject.Find("InputField2").GetComponent<InputField>();  // horizontal angle
        InputText2.onEndEdit.AddListener(GetEnd2);

        InputUL = GameObject.Find("InputField_UL").GetComponent<InputField>();   // up or low
        InputUL.onEndEdit.AddListener(GetEnd3);

    }

    private void Update()
    {
        //_netMqListener.Update();
        changeCamera();
        //Text2.text = "The current tooth number is: " + UL + ", " + txt;
        if (Input.GetKeyDown(KeyCode.A))
        {
            flg = 0;
            TextMode.text = "Current Mode: A (tracker input)";
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            flg = 1;
            TextMode.text = "Current Mode: B (mannual input)";
        }
        if (flg>0)
        {
            HandleMessage2(x,y);
        }
        else if (flg == 0)
        {
            //if (Input.GetKeyDown(KeyCode.D))
            //{
            //    _netMqListener.Update();
            //}
            _netMqListener.Update();
        }

    }

    private void OnDestroy()
    {
        _netMqListener.Stop();
    }
}
 

 
