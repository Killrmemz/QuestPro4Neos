﻿using BaseX;
using FrooxEngine;
using System;

namespace QuestProModule;

public class FbInputDriver : IInputDriver
{
  private readonly SyncCell<FbMessage> _source;
  private FbMessage _c = new();

  private InputInterface _input;
  public int UpdateOrder => 100;
  private Mouth _mouth;
  private Eyes _eyes;

  public float EyeOpenExponent = 1.0f;
  public float EyeWideMulti = 1.0f;
  public float EyeMoveMulti = 1.0f;
  public float EyeExpressionMulti = 1.0f;

  public FbInputDriver(SyncCell<FbMessage> source)
  {
    _source = source;
  }

  /// <summary>
  /// Registers the eye and lip tracking devices with Neos.
  /// </summary>
  /// <param name="list"></param>
  public void CollectDeviceInfos(DataTreeList list)
  {
    var eyeDataTreeDictionary = new DataTreeDictionary();
    eyeDataTreeDictionary.Add("Name", "Quest Pro Eye Tracking");
    eyeDataTreeDictionary.Add("Type", "Eye Tracking");
    eyeDataTreeDictionary.Add("Model", "Quest Pro");
    list.Add(eyeDataTreeDictionary);

    var mouthDataTreeDictionary = new DataTreeDictionary();
    mouthDataTreeDictionary.Add("Name", "Quest Pro Face Tracking");
    mouthDataTreeDictionary.Add("Type", "Lip Tracking");
    mouthDataTreeDictionary.Add("Model", "Quest Pro");
    list.Add(mouthDataTreeDictionary);
  }

  /// <summary>
  /// Sets up the input interfaces for the eyes and mouth data.
  /// </summary>
  /// <param name="inputInterface"></param>
  public void RegisterInputs(InputInterface inputInterface)
  {
    _input = inputInterface;
    _eyes = new Eyes(_input, "Quest Pro Eye Tracking");
    _mouth = new Mouth(_input, "Quest Pro Face Tracking");
  }

  /// <summary>
  /// Gets called every frame to update any inputs.
  /// </summary>
  /// <param name="deltaTime"></param>
  public void UpdateInputs(float deltaTime)
  {
    _source.Swap(ref _c);

    UpdateEyes(deltaTime);
    UpdateMouth();
  }

  private static bool IsValid(float3 value) => IsValid(value.x) && IsValid(value.y) && IsValid(value.z);

  private static bool IsValid(floatQ value) => IsValid(value.x) && IsValid(value.y) && IsValid(value.z) &&
                                               IsValid(value.w) && InRange(value.x, new float2(1, -1)) &&
                                               InRange(value.y, new float2(1, -1)) &&
                                               InRange(value.z, new float2(1, -1)) &&
                                               InRange(value.w, new float2(1, -1));

  private static bool IsValid(float value) => !float.IsInfinity(value) && !float.IsNaN(value);

  private static bool InRange(float value, float2 range) => value <= range.x && value >= range.y;

  /// <summary>
  /// Updates our eye tracking data.
  /// </summary>
  public void UpdateEyes(float deltaTime)
  {
    _eyes.IsEyeTrackingActive = _input.VR_Active;
    _eyes.LeftEye.IsTracking = _input.VR_Active;

    var leftEyeData = GetEyeData(FbEye.Left);
    var rightEyeData = GetEyeData(FbEye.Right);

    _eyes.LeftEye.IsTracking = leftEyeData.IsValid;
    _eyes.LeftEye.RawPosition = leftEyeData.Position;
    _eyes.LeftEye.PupilDiameter = 0.004f;
    _eyes.LeftEye.Squeeze = leftEyeData.Squeeze;
    _eyes.LeftEye.Frown = _c.Expressions[(int)FaceFb.LipCornerPullerL] -
                          _c.Expressions[(int)FaceFb.LipCornerDepressorL] * EyeExpressionMulti;

    UpdateEye(_eyes.LeftEye, leftEyeData);

    _eyes.RightEye.IsTracking = rightEyeData.IsValid;
    _eyes.RightEye.RawPosition = rightEyeData.Position;
    _eyes.RightEye.PupilDiameter = 0.004f;
    _eyes.RightEye.Squeeze = rightEyeData.Squeeze;
    _eyes.RightEye.Frown = _c.Expressions[(int)FaceFb.LipCornerPullerR] -
                           _c.Expressions[(int)FaceFb.LipCornerDepressorR] * EyeExpressionMulti;

    UpdateEye(_eyes.RightEye, rightEyeData);

    if (_eyes.LeftEye.IsTracking ||
        _eyes.RightEye.IsTracking && (!_eyes.LeftEye.IsTracking || !_eyes.RightEye.IsTracking))
    {
      if (_eyes.LeftEye.IsTracking)
      {
        _eyes.CombinedEye.RawPosition = _eyes.LeftEye.RawPosition;
        _eyes.CombinedEye.UpdateWithRotation(_eyes.LeftEye.RawRotation);
      }
      else
      {
        _eyes.CombinedEye.RawPosition = _eyes.RightEye.RawPosition;
        _eyes.CombinedEye.UpdateWithRotation(_eyes.RightEye.RawRotation);
      }

      _eyes.CombinedEye.IsTracking = true;
    }
    else
    {
      _eyes.CombinedEye.IsTracking = false;
    }

    _eyes.CombinedEye.IsTracking = _eyes.LeftEye.IsTracking || _eyes.RightEye.IsTracking;
    _eyes.CombinedEye.RawPosition = (_eyes.LeftEye.RawPosition + _eyes.RightEye.RawPosition) * 0.5f;
    _eyes.CombinedEye.UpdateWithRotation(MathX.Slerp(_eyes.LeftEye.RawRotation, _eyes.RightEye.RawRotation, 0.5f));
    _eyes.CombinedEye.PupilDiameter = 0.004f;

    _eyes.LeftEye.Openness = MathX.Pow(_eyes.LeftEye.Openness, EyeOpenExponent);
    _eyes.RightEye.Openness = MathX.Pow(_eyes.RightEye.Openness, EyeOpenExponent);

    _eyes.ComputeCombinedEyeParameters();
    _eyes.ConvergenceDistance = 0f;
    _eyes.Timestamp += deltaTime;
    _eyes.FinishUpdate();
  }

  private void UpdateEye(Eye eye, EyeGazeData data)
  {
    bool isValid = IsValid(data.Open);
    isValid &= IsValid(data.Position);
    isValid &= IsValid(data.Wide);
    isValid &= IsValid(data.Squeeze);
    isValid &= IsValid(data.Rotation);
    isValid &= eye.IsTracking;

    eye.IsTracking = isValid;

    if (eye.IsTracking)
    {
      eye.UpdateWithRotation(MathX.Slerp(floatQ.Identity, data.Rotation, EyeMoveMulti));
      eye.Openness = MathX.Pow(MathX.FilterInvalid(data.Open, 0.0f), EyeOpenExponent);
      eye.Widen = data.Wide * EyeWideMulti;
    }
  }


  public EyeGazeData GetEyeData(FbEye fbEye)
  {
    EyeGazeData eyeRet = new EyeGazeData();
    switch (fbEye)
    {
      case FbEye.Left:
        eyeRet.Position = new float3(_c.Expressions[FbExpression.LeftPos_x], -_c.Expressions[FbExpression.LeftPos_y],
          _c.Expressions[FbExpression.LeftPos_z]);
        eyeRet.Rotation = new floatQ(-_c.Expressions[FbExpression.LeftRot_x], -_c.Expressions[FbExpression.LeftRot_y],
          -_c.Expressions[FbExpression.LeftRot_z], _c.Expressions[FbExpression.LeftRot_w]);
        eyeRet.Open = MathX.Max(0, _c.Expressions[(int)FaceFb.EyesClosedL]);
        eyeRet.Squeeze = _c.Expressions[(int)FaceFb.LidTightenerL];
        eyeRet.Wide = _c.Expressions[(int)FaceFb.UpperLidRaiserL];
        eyeRet.IsValid = IsValid(eyeRet.Position);
        return eyeRet;
      case FbEye.Right:
        eyeRet.Position = new float3(_c.Expressions[FbExpression.RightPos_x], -_c.Expressions[FbExpression.RightPos_y],
          _c.Expressions[FbExpression.RightPos_z]);
        eyeRet.Rotation = new floatQ(-_c.Expressions[FbExpression.LeftRot_x], -_c.Expressions[FbExpression.LeftRot_y],
          -_c.Expressions[FbExpression.LeftRot_z], _c.Expressions[FbExpression.RightRot_w]);
        eyeRet.Open = MathX.Max(0, _c.Expressions[(int)FaceFb.EyesClosedR]);
        eyeRet.Squeeze = _c.Expressions[(int)FaceFb.LidTightenerR];
        eyeRet.Wide = _c.Expressions[(int)FaceFb.UpperLidRaiserR];
        eyeRet.IsValid = IsValid(eyeRet.Position);
        return eyeRet;
      default:
        throw new Exception($"Invalid eye argument: {fbEye}");
    }
  }

  public void UpdateMouth()
  {
    _mouth.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
    _mouth.IsTracking = Engine.Current.InputInterface.VR_Active;

    _mouth.JawOpen = _c.Expressions[(int)FaceFb.JawDrop];

    var jawHorizontal = _c.Expressions[(int)FaceFb.JawSidewaysRight] -
                        _c.Expressions[(int)FaceFb.JawSidewaysLeft];
    var jawForward = _c.Expressions[(int)FaceFb.JawThrust];
    var jawDown = _c.Expressions[(int)FaceFb.LipsToward] + _c.Expressions[(int)FaceFb.JawDrop];

    _mouth.Jaw = new float3(
      jawHorizontal,
      jawForward,
      jawDown
    );

    _mouth.LipUpperLeftRaise = _c.Expressions[(int)FaceFb.UpperLipRaiserL];
    _mouth.LipUpperRightRaise = _c.Expressions[(int)FaceFb.UpperLipRaiserR];
    _mouth.LipLowerLeftRaise = _c.Expressions[(int)FaceFb.LowerLipDepressorL];
    _mouth.LipLowerRightRaise = _c.Expressions[(int)FaceFb.LowerLipDepressorR];

    _mouth.LipUpperHorizontal = _c.Expressions[(int)FaceFb.MouthRight] - _c.Expressions[(int)FaceFb.MouthLeft];
    _mouth.LipLowerHorizontal = _c.Expressions[(int)FaceFb.MouthRight] - _c.Expressions[(int)FaceFb.MouthLeft];

    _mouth.MouthLeftSmileFrown = _c.Expressions[(int)FaceFb.LipCornerPullerL] -
                                 _c.Expressions[(int)FaceFb.LipCornerDepressorL];
    _mouth.MouthRightSmileFrown = _c.Expressions[(int)FaceFb.LipCornerPullerR] -
                                  _c.Expressions[(int)FaceFb.LipCornerDepressorR];

    _mouth.MouthPout = _c.Expressions[(int)FaceFb.LipPuckerL] + _c.Expressions[(int)FaceFb.LipPuckerR];

    _mouth.LipTopOverturn = _c.Expressions[(int)FaceFb.LipFunnelerRT] + _c.Expressions[(int)FaceFb.LipFunnelerLT];
    _mouth.LipBottomOverturn =
      _c.Expressions[(int)FaceFb.LipFunnelerRB] + _c.Expressions[(int)FaceFb.LipFunnelerLB];

    _mouth.LipTopOverUnder = -(_c.Expressions[(int)FaceFb.LipSuckRT] + _c.Expressions[(int)FaceFb.LipSuckLT]);
    _mouth.LipBottomOverUnder = _c.Expressions[(int)FaceFb.ChinRaiserB] -
                                (_c.Expressions[(int)FaceFb.LipSuckRB] + _c.Expressions[(int)FaceFb.LipSuckLB]);

    _mouth.CheekLeftPuffSuck = _c.Expressions[(int)FaceFb.CheekPuffL];
    _mouth.CheekRightPuffSuck = _c.Expressions[(int)FaceFb.CheekPuffR];

    _mouth.CheekLeftPuffSuck -= _c.Expressions[(int)FaceFb.CheekSuckL];
    _mouth.CheekRightPuffSuck -= _c.Expressions[(int)FaceFb.CheekSuckR];
  }

  public struct EyeGazeData
  {
    public bool IsValid;
    public float3 Position;
    public floatQ Rotation;
    public float Open;
    public float Squeeze;
    public float Wide;
    public float GazeConfidence;
  }

  public enum FbEye
  {
    Left,
    Right,
    Combined
  }
}
