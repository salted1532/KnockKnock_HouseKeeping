Shader "Wreckreation Games/Procedural Skybox"
{
    Properties
    {
        _Tint("Tint", Color) = (1, 1, 1, 0.5)
        _Sky_Color("Sky Color", Color) = (0.4666409, 0.6906381, 0.823, 0)
        _Exposure("Exposure", Range(0, 4)) = 1
        _Light_Pollution("Light Pollution", Range(0, 1)) = 0
        _Sun_Size("Sun Size", Range(0, 0.6)) = 0
        _Haze_Strength("Haze Strength", Range(0, 1)) = 0
        [NoScaleOffset]_Starmap("Starmap", CUBE) = "" {}
        _Star_Roration_Speed("Star Roration Speed", Range(-1, 1)) = 0.06
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Background"
            "UniversalMaterialType" = "Unlit"
            "Queue"="Background"
            "DisableBatching"="True"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalUnlitSubTarget"
            "PreviewType"="Skybox"

        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                // LightMode: <None>
            }
        
        // Render State
        Cull Back
        Blend off
        ZTest LEqual
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma shader_feature _ _SAMPLE_GI
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_UNLIT
        #define _FOG_FRAGMENT 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceViewDirection;
             float3 WorldSpaceViewDirection;
             float3 WorldSpacePosition;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS : INTERP0;
             float3 normalWS : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Sky_Color;
        float _Exposure;
        float4 _Tint;
        float _Sun_Size;
        float _Haze_Strength;
        float _Light_Pollution;
        float _Star_Roration_Speed;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURECUBE(_Starmap);
        SAMPLER(sampler_Starmap);
        
        // Graph Includes
        #include "Assets/WG Free Skies/Skyboxes/URP Procedural Skybox/CustomFunctions/GetMainLight.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void MainLightDirection_float(out float3 Direction)
        {
            #if SHADERGRAPH_PREVIEW
            Direction = half3(-0.5, -0.5, 0);
            #else
            Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
            #endif
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float(Bindings_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float IN, out float OutVector1_1)
        {
        float3 _MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3);
        float3 _Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3);
        float3 _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3, float3(-1, -1, -1), _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3);
        float _DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float;
        Unity_DotProduct_float3(_MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3, _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3, _DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float);
        float _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float;
        Unity_Saturate_float(_DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float, _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float);
        OutVector1_1 = _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        struct Bindings_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float(float3 _WorldPos, bool _WorldPos_a8754bc66eea418fb6c8b3ac5ee821e6_IsConnected, Bindings_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float IN, out float3 Direction_1, out float3 Color_2, out float ShadowAtten_3)
        {
        float3 _Property_da55b32ae35a4271a77c0649c9147c66_Out_0_Vector3 = _WorldPos;
        bool _Property_da55b32ae35a4271a77c0649c9147c66_Out_0_Vector3_IsConnected = _WorldPos_a8754bc66eea418fb6c8b3ac5ee821e6_IsConnected;
        float3 _BranchOnInputConnection_2e680a118c3a482aa321329faf04e029_Out_3_Vector3 = _Property_da55b32ae35a4271a77c0649c9147c66_Out_0_Vector3_IsConnected ? _Property_da55b32ae35a4271a77c0649c9147c66_Out_0_Vector3 : IN.WorldSpacePosition;
        float3 _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Direction_2_Vector3;
        float3 _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Color_1_Vector3;
        float _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_ShadowAtten_3_Float;
        GetMainLight_float(_BranchOnInputConnection_2e680a118c3a482aa321329faf04e029_Out_3_Vector3, _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Direction_2_Vector3, _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Color_1_Vector3, _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_ShadowAtten_3_Float);
        Direction_1 = _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Direction_2_Vector3;
        Color_2 = _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Color_1_Vector3;
        ShadowAtten_3 = _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_ShadowAtten_3_Float;
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        void Unity_Maximum_float3(float3 A, float3 B, out float3 Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Ceiling_float3(float3 In, out float3 Out)
        {
            Out = ceil(In);
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        struct Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float IN, out float OutVector1_1)
        {
        float3 _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3);
        float _Split_3f1176004e8b43af98a33a196b09074f_R_1_Float = _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3[0];
        float _Split_3f1176004e8b43af98a33a196b09074f_G_2_Float = _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3[1];
        float _Split_3f1176004e8b43af98a33a196b09074f_B_3_Float = _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3[2];
        float _Split_3f1176004e8b43af98a33a196b09074f_A_4_Float = 0;
        float _Multiply_78434ea90a094b52afd20ccb59993614_Out_2_Float;
        Unity_Multiply_float_float(_Split_3f1176004e8b43af98a33a196b09074f_G_2_Float, -1, _Multiply_78434ea90a094b52afd20ccb59993614_Out_2_Float);
        float _Saturate_a5d3f27860cb43829ec8334a9c7621a9_Out_1_Float;
        Unity_Saturate_float(_Multiply_78434ea90a094b52afd20ccb59993614_Out_2_Float, _Saturate_a5d3f27860cb43829ec8334a9c7621a9_Out_1_Float);
        float _OneMinus_5c142230ac4c453793d389e55063c90b_Out_1_Float;
        Unity_OneMinus_float(_Saturate_a5d3f27860cb43829ec8334a9c7621a9_Out_1_Float, _OneMinus_5c142230ac4c453793d389e55063c90b_Out_1_Float);
        float _Step_468d240d2ff2400d8c04b71c15e4a886_Out_2_Float;
        Unity_Step_float(float(1), _OneMinus_5c142230ac4c453793d389e55063c90b_Out_1_Float, _Step_468d240d2ff2400d8c04b71c15e4a886_Out_2_Float);
        float _Saturate_e1b2e9e85e434bdfb40a8be726cfbdb4_Out_1_Float;
        Unity_Saturate_float(_Step_468d240d2ff2400d8c04b71c15e4a886_Out_2_Float, _Saturate_e1b2e9e85e434bdfb40a8be726cfbdb4_Out_1_Float);
        OutVector1_1 = _Saturate_e1b2e9e85e434bdfb40a8be726cfbdb4_Out_1_Float;
        }
        
        void Unity_Rotate_About_Axis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
        {
            Rotation = radians(Rotation);
        
            float s = sin(Rotation);
            float c = cos(Rotation);
            float one_minus_c = 1.0 - c;
        
            Axis = normalize(Axis);
        
            float3x3 rot_mat = { one_minus_c * Axis.x * Axis.x + c,            one_minus_c * Axis.x * Axis.y - Axis.z * s,     one_minus_c * Axis.z * Axis.x + Axis.y * s,
                                      one_minus_c * Axis.x * Axis.y + Axis.z * s,   one_minus_c * Axis.y * Axis.y + c,              one_minus_c * Axis.y * Axis.z - Axis.x * s,
                                      one_minus_c * Axis.z * Axis.x - Axis.y * s,   one_minus_c * Axis.y * Axis.z + Axis.x * s,     one_minus_c * Axis.z * Axis.z + c
                                    };
        
            Out = mul(rot_mat,  In);
        }
        
        void Unity_CrossProduct_float(float3 A, float3 B, out float3 Out)
        {
            Out = cross(A, B);
        }
        
        struct Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float
        {
        float3 WorldSpaceViewDirection;
        };
        
        void SG_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float(float3 _Axis, float _Angle, float _LightAngle, float3 _Direction_Mask, float3 _Reference_Direction, float _Rotation, Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float IN, out float3 Out_1, out float3 Mask_2, out float3 Reference_Direction_3)
        {
        float3 _MainLightDirection_3f978ad70c274d23b6d8e1b2cc0f9599_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_3f978ad70c274d23b6d8e1b2cc0f9599_Direction_0_Vector3);
        float _Property_11f9a030df9f44aa84e5f7b3c2c61320_Out_0_Float = _Rotation;
        float _Multiply_ce46e0dddd1044f09ef7f44bedb27b67_Out_2_Float;
        Unity_Multiply_float_float(_Property_11f9a030df9f44aa84e5f7b3c2c61320_Out_0_Float, 2, _Multiply_ce46e0dddd1044f09ef7f44bedb27b67_Out_2_Float);
        float3 _RotateAboutAxis_dc58e436703348b59b729c00aa426b02_Out_3_Vector3;
        Unity_Rotate_About_Axis_Degrees_float(_MainLightDirection_3f978ad70c274d23b6d8e1b2cc0f9599_Direction_0_Vector3, float3 (0, -1, 0), _Multiply_ce46e0dddd1044f09ef7f44bedb27b67_Out_2_Float, _RotateAboutAxis_dc58e436703348b59b729c00aa426b02_Out_3_Vector3);
        float3 _Property_7bfa7986748e4f53adfa9a12c3d3fe12_Out_0_Vector3 = _Axis;
        float _Property_5ea44cf579454cb781eb086b6d3be43b_Out_0_Float = _LightAngle;
        float3 _RotateAboutAxis_73980326b96a4b3e9824340784e77c96_Out_3_Vector3;
        Unity_Rotate_About_Axis_Degrees_float(_RotateAboutAxis_dc58e436703348b59b729c00aa426b02_Out_3_Vector3, _Property_7bfa7986748e4f53adfa9a12c3d3fe12_Out_0_Vector3, _Property_5ea44cf579454cb781eb086b6d3be43b_Out_0_Float, _RotateAboutAxis_73980326b96a4b3e9824340784e77c96_Out_3_Vector3);
        float _Property_7402c98ce263489f9c6d8d8b19a42690_Out_0_Float = _Angle;
        float3 _RotateAboutAxis_20f73daa95264608b2a0ebff411dacd7_Out_3_Vector3;
        Unity_Rotate_About_Axis_Degrees_float(IN.WorldSpaceViewDirection, _Property_7bfa7986748e4f53adfa9a12c3d3fe12_Out_0_Vector3, _Property_7402c98ce263489f9c6d8d8b19a42690_Out_0_Float, _RotateAboutAxis_20f73daa95264608b2a0ebff411dacd7_Out_3_Vector3);
        float3 _Property_c17dbfea91114909b928e649fc67f177_Out_0_Vector3 = _Direction_Mask;
        float3 _Multiply_7f9de497dee84d6ca47e9e50c5b283cb_Out_2_Vector3;
        Unity_Multiply_float3_float3(_RotateAboutAxis_20f73daa95264608b2a0ebff411dacd7_Out_3_Vector3, _Property_c17dbfea91114909b928e649fc67f177_Out_0_Vector3, _Multiply_7f9de497dee84d6ca47e9e50c5b283cb_Out_2_Vector3);
        float3 _CrossProduct_c8cb8c20650a480f9f3a9ecfb823790f_Out_2_Vector3;
        Unity_CrossProduct_float(_RotateAboutAxis_73980326b96a4b3e9824340784e77c96_Out_3_Vector3, _Multiply_7f9de497dee84d6ca47e9e50c5b283cb_Out_2_Vector3, _CrossProduct_c8cb8c20650a480f9f3a9ecfb823790f_Out_2_Vector3);
        float3 _Property_f6aa6b80c0d143758a949788b3b91333_Out_0_Vector3 = _Reference_Direction;
        float3 _Multiply_118a95527ea74b319266cf0312071080_Out_2_Vector3;
        Unity_Multiply_float3_float3(IN.WorldSpaceViewDirection, _Property_f6aa6b80c0d143758a949788b3b91333_Out_0_Vector3, _Multiply_118a95527ea74b319266cf0312071080_Out_2_Vector3);
        float3 _Multiply_d1ec4933ebc14085afd38f962b90902f_Out_2_Vector3;
        Unity_Multiply_float3_float3(_CrossProduct_c8cb8c20650a480f9f3a9ecfb823790f_Out_2_Vector3, _Multiply_118a95527ea74b319266cf0312071080_Out_2_Vector3, _Multiply_d1ec4933ebc14085afd38f962b90902f_Out_2_Vector3);
        Out_1 = _Multiply_d1ec4933ebc14085afd38f962b90902f_Out_2_Vector3;
        Mask_2 = _CrossProduct_c8cb8c20650a480f9f3a9ecfb823790f_Out_2_Vector3;
        Reference_Direction_3 = _Multiply_118a95527ea74b319266cf0312071080_Out_2_Vector3;
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        struct Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float
        {
        };
        
        void SG_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float(float _Single_Channel, Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float IN, out float Mask_Integral_1, out float Axis_2, out float Neg_Axis_3)
        {
        float _Property_021e4e3a38294a489e1ff0f55bd1c796_Out_0_Float = _Single_Channel;
        float _Clamp_d47adc1041f743a9806830337d36b762_Out_3_Float;
        Unity_Clamp_float(_Property_021e4e3a38294a489e1ff0f55bd1c796_Out_0_Float, float(0), float(1), _Clamp_d47adc1041f743a9806830337d36b762_Out_3_Float);
        float _Remap_defdc492c647436088c64df42e34f072_Out_3_Float;
        Unity_Remap_float(_Property_021e4e3a38294a489e1ff0f55bd1c796_Out_0_Float, float2 (-1, 1), float2 (1, -1), _Remap_defdc492c647436088c64df42e34f072_Out_3_Float);
        float _Clamp_02106530d3464895a9f4c87814088e67_Out_3_Float;
        Unity_Clamp_float(_Remap_defdc492c647436088c64df42e34f072_Out_3_Float, float(0), float(1), _Clamp_02106530d3464895a9f4c87814088e67_Out_3_Float);
        float _Add_2af9063bdac24d59a5077f7797ab02eb_Out_2_Float;
        Unity_Add_float(_Clamp_d47adc1041f743a9806830337d36b762_Out_3_Float, _Clamp_02106530d3464895a9f4c87814088e67_Out_3_Float, _Add_2af9063bdac24d59a5077f7797ab02eb_Out_2_Float);
        Mask_Integral_1 = _Add_2af9063bdac24d59a5077f7797ab02eb_Out_2_Float;
        Axis_2 = _Clamp_d47adc1041f743a9806830337d36b762_Out_3_Float;
        Neg_Axis_3 = _Clamp_02106530d3464895a9f4c87814088e67_Out_3_Float;
        }
        
        struct Bindings_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float
        {
        };
        
        void SG_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float(float _F1, float _F2, float _F3, float _Clamp, Bindings_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float IN, out float Out_1)
        {
        float _Property_bc6007b810004657a6743d108cb7122a_Out_0_Float = _F1;
        float _Property_60a7b711175b4f5093b54107b450564c_Out_0_Float = _F2;
        float _Add_a2f39a5f48354341992976d8d319eae1_Out_2_Float;
        Unity_Add_float(_Property_bc6007b810004657a6743d108cb7122a_Out_0_Float, _Property_60a7b711175b4f5093b54107b450564c_Out_0_Float, _Add_a2f39a5f48354341992976d8d319eae1_Out_2_Float);
        float _Property_0c2b6cf8d0834b34b999acbbb5028f14_Out_0_Float = _F3;
        float _Add_e1fde91e48f243bfba8cb8909aefedcf_Out_2_Float;
        Unity_Add_float(_Add_a2f39a5f48354341992976d8d319eae1_Out_2_Float, _Property_0c2b6cf8d0834b34b999acbbb5028f14_Out_0_Float, _Add_e1fde91e48f243bfba8cb8909aefedcf_Out_2_Float);
        Out_1 = _Add_e1fde91e48f243bfba8cb8909aefedcf_Out_2_Float;
        }
        
        struct Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float
        {
        float3 WorldSpaceViewDirection;
        };
        
        void SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(float _Power, float _Rotation, Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float IN, out float Axis_1, out float Neg_Axis_2)
        {
        float3 _Vector3_558649f6bb6047ecae9676e2bba7c29a_Out_0_Vector3 = float3(float(0), float(1), float(0));
        float _Float_e6cd52f775e94a2c8a3c67562f1d650f_Out_0_Float = float(90);
        float3 _Vector3_5c17e80b8dff472b9e2517353541e86e_Out_0_Vector3 = float3(float(0), float(0), float(1));
        float3 _Vector3_5df26d2e743848f5aabbcaf4ede22863_Out_0_Vector3 = float3(float(1), float(0), float(0));
        float _Property_8b9491b137964a47b72d0c659f60a15c_Out_0_Float = _Rotation;
        Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5;
        _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3;
        half3 _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Mask_2_Vector3;
        half3 _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_ReferenceDirection_3_Vector3;
        SG_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float(_Vector3_558649f6bb6047ecae9676e2bba7c29a_Out_0_Vector3, _Float_e6cd52f775e94a2c8a3c67562f1d650f_Out_0_Float, _Float_e6cd52f775e94a2c8a3c67562f1d650f_Out_0_Float, _Vector3_5c17e80b8dff472b9e2517353541e86e_Out_0_Vector3, _Vector3_5df26d2e743848f5aabbcaf4ede22863_Out_0_Vector3, _Property_8b9491b137964a47b72d0c659f60a15c_Out_0_Float, _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5, _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3, _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Mask_2_Vector3, _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_ReferenceDirection_3_Vector3);
        float _Split_89a273ee7bcd4b6ebede46ccbe2d595e_R_1_Float = _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3[0];
        float _Split_89a273ee7bcd4b6ebede46ccbe2d595e_G_2_Float = _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3[1];
        float _Split_89a273ee7bcd4b6ebede46ccbe2d595e_B_3_Float = _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3[2];
        float _Split_89a273ee7bcd4b6ebede46ccbe2d595e_A_4_Float = 0;
        Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9;
        half _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_MaskIntegral_1_Float;
        half _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_Axis_2_Float;
        half _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_NegAxis_3_Float;
        SG_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float(_Split_89a273ee7bcd4b6ebede46ccbe2d595e_R_1_Float, _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9, _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_MaskIntegral_1_Float, _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_Axis_2_Float, _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_NegAxis_3_Float);
        float3 _Vector3_e94d1720a60b4038aab5b0365a843891_Out_0_Vector3 = float3(float(0), float(1), float(0));
        float _Float_1777c8b349984742adc51c84c3cb32c9_Out_0_Float = float(90);
        float3 _Vector3_ad840f874e2649f28c7082718003a36d_Out_0_Vector3 = float3(float(1), float(0), float(0));
        float3 _Vector3_dc33e0531d0d459ca8bf02b7a2cf408e_Out_0_Vector3 = float3(float(0), float(0), float(1));
        Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d;
        _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3;
        half3 _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Mask_2_Vector3;
        half3 _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_ReferenceDirection_3_Vector3;
        SG_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float(_Vector3_e94d1720a60b4038aab5b0365a843891_Out_0_Vector3, _Float_1777c8b349984742adc51c84c3cb32c9_Out_0_Float, _Float_1777c8b349984742adc51c84c3cb32c9_Out_0_Float, _Vector3_ad840f874e2649f28c7082718003a36d_Out_0_Vector3, _Vector3_dc33e0531d0d459ca8bf02b7a2cf408e_Out_0_Vector3, _Property_8b9491b137964a47b72d0c659f60a15c_Out_0_Float, _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d, _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3, _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Mask_2_Vector3, _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_ReferenceDirection_3_Vector3);
        float _Split_1d631d3e539647e6b433a95122d49fb4_R_1_Float = _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3[0];
        float _Split_1d631d3e539647e6b433a95122d49fb4_G_2_Float = _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3[1];
        float _Split_1d631d3e539647e6b433a95122d49fb4_B_3_Float = _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3[2];
        float _Split_1d631d3e539647e6b433a95122d49fb4_A_4_Float = 0;
        Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40;
        half _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_MaskIntegral_1_Float;
        half _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_Axis_2_Float;
        half _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_NegAxis_3_Float;
        SG_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float(_Split_1d631d3e539647e6b433a95122d49fb4_B_3_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_MaskIntegral_1_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_Axis_2_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_NegAxis_3_Float);
        float3 _Vector3_1f34ecf82e914ca5bcee1bef0b9ef3fe_Out_0_Vector3 = float3(float(1), float(0), float(0));
        float _Float_ddd7e4715cf94f0aa27ff5b5e9cf660f_Out_0_Float = float(90);
        float3 _Vector3_431fd60e4b184036b6b6f257448fff42_Out_0_Vector3 = float3(float(0), float(0), float(1));
        float3 _Vector3_de948620452742bfb19d3eb02856f4a7_Out_0_Vector3 = float3(float(0), float(-1), float(0));
        Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float _DirectionMask_e7bee712463546a09117207b42c4d099;
        _DirectionMask_e7bee712463546a09117207b42c4d099.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _DirectionMask_e7bee712463546a09117207b42c4d099_Out_1_Vector3;
        half3 _DirectionMask_e7bee712463546a09117207b42c4d099_Mask_2_Vector3;
        half3 _DirectionMask_e7bee712463546a09117207b42c4d099_ReferenceDirection_3_Vector3;
        SG_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float(_Vector3_1f34ecf82e914ca5bcee1bef0b9ef3fe_Out_0_Vector3, _Float_ddd7e4715cf94f0aa27ff5b5e9cf660f_Out_0_Float, half(0), _Vector3_431fd60e4b184036b6b6f257448fff42_Out_0_Vector3, _Vector3_de948620452742bfb19d3eb02856f4a7_Out_0_Vector3, _Property_8b9491b137964a47b72d0c659f60a15c_Out_0_Float, _DirectionMask_e7bee712463546a09117207b42c4d099, _DirectionMask_e7bee712463546a09117207b42c4d099_Out_1_Vector3, _DirectionMask_e7bee712463546a09117207b42c4d099_Mask_2_Vector3, _DirectionMask_e7bee712463546a09117207b42c4d099_ReferenceDirection_3_Vector3);
        float4 _Swizzle_238403211f7e48bdbb15ed1d20a95689_Out_1_Vector4 = _DirectionMask_e7bee712463546a09117207b42c4d099_Mask_2_Vector3.yxzz;
        float3 _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Swizzle_238403211f7e48bdbb15ed1d20a95689_Out_1_Vector4.xyz), _DirectionMask_e7bee712463546a09117207b42c4d099_ReferenceDirection_3_Vector3, _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3);
        float _Split_34e340c38b4d45f087e78448338f6d88_R_1_Float = _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3[0];
        float _Split_34e340c38b4d45f087e78448338f6d88_G_2_Float = _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3[1];
        float _Split_34e340c38b4d45f087e78448338f6d88_B_3_Float = _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3[2];
        float _Split_34e340c38b4d45f087e78448338f6d88_A_4_Float = 0;
        Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float _MaskModulator_4298e77c3b6c46758ca7300e7e78663a;
        half _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_MaskIntegral_1_Float;
        half _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_Axis_2_Float;
        half _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_NegAxis_3_Float;
        SG_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float(_Split_34e340c38b4d45f087e78448338f6d88_G_2_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_MaskIntegral_1_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_Axis_2_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_NegAxis_3_Float);
        Bindings_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float _TriAdditiveFloat_f0321225145e4a639f238d1760d97c03;
        half _TriAdditiveFloat_f0321225145e4a639f238d1760d97c03_Out_1_Float;
        SG_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float(_MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_Axis_2_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_Axis_2_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_Axis_2_Float, 0, _TriAdditiveFloat_f0321225145e4a639f238d1760d97c03, _TriAdditiveFloat_f0321225145e4a639f238d1760d97c03_Out_1_Float);
        float _Property_7b14e93a22e54d68b88fb6bf74202885_Out_0_Float = _Power;
        float _Power_6f359598b5674a5aad9b2689d24b4f33_Out_2_Float;
        Unity_Power_float(_TriAdditiveFloat_f0321225145e4a639f238d1760d97c03_Out_1_Float, _Property_7b14e93a22e54d68b88fb6bf74202885_Out_0_Float, _Power_6f359598b5674a5aad9b2689d24b4f33_Out_2_Float);
        Bindings_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float _TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77;
        half _TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77_Out_1_Float;
        SG_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float(_MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_NegAxis_3_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_NegAxis_3_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_NegAxis_3_Float, 0, _TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77, _TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77_Out_1_Float);
        float _Power_c263b74a39ca4a9ba1c608ce78fbbf3c_Out_2_Float;
        Unity_Power_float(_TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77_Out_1_Float, _Property_7b14e93a22e54d68b88fb6bf74202885_Out_0_Float, _Power_c263b74a39ca4a9ba1c608ce78fbbf3c_Out_2_Float);
        Axis_1 = _Power_6f359598b5674a5aad9b2689d24b4f33_Out_2_Float;
        Neg_Axis_2 = _Power_c263b74a39ca4a9ba1c608ce78fbbf3c_Out_2_Float;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Lerp_float(float A, float B, float T, out float Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
        {
            // convert to OkLab if we need perceptual color space.
            float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);
        
            [unroll]
            for (int c = 1; c < Gradient.colorsLength; c++)
            {
                float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
                float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
                color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
            }
            color = lerp(color, OklabToLinear(color), Gradient.type == 2);
        
        #ifdef UNITY_COLORSPACE_GAMMA
            color = LinearToSRGB(color);
        #endif
        
            float alpha = Gradient.alphas[0].x;
            [unroll]
            for (int a = 1; a < Gradient.alphasLength; a++)
            {
                float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
                alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
            }
        
            Out = float4(color, alpha);
        }
        
        struct Bindings_IHHSubGraph_210e4d2b552b95f439d320382950a945_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_IHHSubGraph_210e4d2b552b95f439d320382950a945_float(Bindings_IHHSubGraph_210e4d2b552b95f439d320382950a945_float IN, out float OutVector1_1)
        {
        float3 _MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3);
        float3 _Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3);
        float3 _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3, float3(-1, 0, -1), _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3);
        float _DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float;
        Unity_DotProduct_float3(_MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3, _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3, _DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float);
        float _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float;
        Unity_Saturate_float(_DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float, _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float);
        OutVector1_1 = _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float;
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Clamp_float4(float4 In, float4 Min, float4 Max, out float4 Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Power_float4(float4 A, float4 B, out float4 Out)
        {
            Out = pow(A, B);
        }
        
        struct Bindings_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float
        {
        float3 ObjectSpaceNormal;
        float3 ObjectSpaceViewDirection;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float3 TimeParameters;
        };
        
        void SG_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float(UnityTextureCube _Starmap, float4 _Sky_Color, float _Exposure, float4 _Tint, float _Sun_Size, float _Cloud_Mask, float _Light_Pollution, float _Haze_Strength, Gradient _Gradient, float _Star_Roration_Speed, Bindings_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float IN, out float3 OutVector3_1)
        {
        float _Float_e4fd19b52466439fa21635069f45b430_Out_0_Float = float(0.95);
        float _Property_59196564f99a4a8fac46ecaeafaf30be_Out_0_Float = _Sun_Size;
        float _Subtract_ff5a72976c724699a42ada81eed745ae_Out_2_Float;
        Unity_Subtract_float(_Float_e4fd19b52466439fa21635069f45b430_Out_0_Float, _Property_59196564f99a4a8fac46ecaeafaf30be_Out_0_Float, _Subtract_ff5a72976c724699a42ada81eed745ae_Out_2_Float);
        float _Multiply_b61ae6740e7645dcb5a976e3dc736f46_Out_2_Float;
        Unity_Multiply_float_float(_Property_59196564f99a4a8fac46ecaeafaf30be_Out_0_Float, 0.01, _Multiply_b61ae6740e7645dcb5a976e3dc736f46_Out_2_Float);
        float _Power_d50dca165d524c15aa71b6ecc3b32de5_Out_2_Float;
        Unity_Power_float(_Multiply_b61ae6740e7645dcb5a976e3dc736f46_Out_2_Float, float(0.5), _Power_d50dca165d524c15aa71b6ecc3b32de5_Out_2_Float);
        float _Add_fb0d23f2b0dd4f4b8fdf7078e8a2117e_Out_2_Float;
        Unity_Add_float(_Subtract_ff5a72976c724699a42ada81eed745ae_Out_2_Float, _Power_d50dca165d524c15aa71b6ecc3b32de5_Out_2_Float, _Add_fb0d23f2b0dd4f4b8fdf7078e8a2117e_Out_2_Float);
        Bindings_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float _SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940;
        _SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940.WorldSpacePosition = IN.WorldSpacePosition;
        float _SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940_OutVector1_1_Float;
        SG_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float(_SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940, _SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940_OutVector1_1_Float);
        float _Power_7ec402ff3ffe491898f147559ae6f309_Out_2_Float;
        Unity_Power_float(_SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940_OutVector1_1_Float, float(60), _Power_7ec402ff3ffe491898f147559ae6f309_Out_2_Float);
        float _Smoothstep_fac4d918b1bb4f96ad318fd7d025e4e1_Out_3_Float;
        Unity_Smoothstep_float(_Subtract_ff5a72976c724699a42ada81eed745ae_Out_2_Float, _Add_fb0d23f2b0dd4f4b8fdf7078e8a2117e_Out_2_Float, _Power_7ec402ff3ffe491898f147559ae6f309_Out_2_Float, _Smoothstep_fac4d918b1bb4f96ad318fd7d025e4e1_Out_3_Float);
        float4 Color_632156c4356f4f12925a118136eb5c05 = IsGammaSpace() ? LinearToSRGB(float4(16, 16, 16, 0)) : float4(16, 16, 16, 0);
        Bindings_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151;
        _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Direction_1_Vector3;
        float3 _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Color_2_Vector3;
        float _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_ShadowAtten_3_Float;
        SG_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float(half3 (0, 0, 0), false, _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151, _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Direction_1_Vector3, _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Color_2_Vector3, _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_ShadowAtten_3_Float);
        float3 _Saturation_96a051ca05a64dcba47d2336f6a8e5ff_Out_2_Vector3;
        Unity_Saturation_float(_GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Color_2_Vector3, float(0), _Saturation_96a051ca05a64dcba47d2336f6a8e5ff_Out_2_Vector3);
        float _Float_b84f90b58b5b4558a94da49ddded5d5d_Out_0_Float = float(0.05);
        float3 _Maximum_579bbfe6a6f343cb9b4330031cdbe8de_Out_2_Vector3;
        Unity_Maximum_float3(_Saturation_96a051ca05a64dcba47d2336f6a8e5ff_Out_2_Vector3, (_Float_b84f90b58b5b4558a94da49ddded5d5d_Out_0_Float.xxx), _Maximum_579bbfe6a6f343cb9b4330031cdbe8de_Out_2_Vector3);
        float3 _Ceiling_4e601f08b30d49e6b238932653d00538_Out_1_Vector3;
        Unity_Ceiling_float3(_Saturation_96a051ca05a64dcba47d2336f6a8e5ff_Out_2_Vector3, _Ceiling_4e601f08b30d49e6b238932653d00538_Out_1_Vector3);
        float _Branch_5aa75ae8578946f1bec3ad4fd4a7a435_Out_3_Float;
        Unity_Branch_float(((bool) _Ceiling_4e601f08b30d49e6b238932653d00538_Out_1_Vector3.x), float(1), float(0), _Branch_5aa75ae8578946f1bec3ad4fd4a7a435_Out_3_Float);
        float3 _Multiply_bd06f1f066fe46d581b744594405f39f_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Maximum_579bbfe6a6f343cb9b4330031cdbe8de_Out_2_Vector3, (_Branch_5aa75ae8578946f1bec3ad4fd4a7a435_Out_3_Float.xxx), _Multiply_bd06f1f066fe46d581b744594405f39f_Out_2_Vector3);
        float3 _Multiply_ba90c1d6677a47ecae920085e7178adb_Out_2_Vector3;
        Unity_Multiply_float3_float3((Color_632156c4356f4f12925a118136eb5c05.xyz), _Multiply_bd06f1f066fe46d581b744594405f39f_Out_2_Vector3, _Multiply_ba90c1d6677a47ecae920085e7178adb_Out_2_Vector3);
        float3 _Multiply_00872863b4e641a4895950d8254775ec_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Smoothstep_fac4d918b1bb4f96ad318fd7d025e4e1_Out_3_Float.xxx), _Multiply_ba90c1d6677a47ecae920085e7178adb_Out_2_Vector3, _Multiply_00872863b4e641a4895950d8254775ec_Out_2_Vector3);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_0b72deb735fc442e8fb0228371f79878;
        _BottomMargin_0b72deb735fc442e8fb0228371f79878.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_0b72deb735fc442e8fb0228371f79878_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_0b72deb735fc442e8fb0228371f79878, _BottomMargin_0b72deb735fc442e8fb0228371f79878_OutVector1_1_Float);
        float3 _Multiply_5944a8d7f2834ef6bf339c32906f13dd_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_00872863b4e641a4895950d8254775ec_Out_2_Vector3, (_BottomMargin_0b72deb735fc442e8fb0228371f79878_OutVector1_1_Float.xxx), _Multiply_5944a8d7f2834ef6bf339c32906f13dd_Out_2_Vector3);
        Gradient _Property_01e6112c6b2441c5b6e6fdac20b773b1_Out_0_Gradient = _Gradient;
        Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float _G_0f0d5716ee584deab6b4e7ee1dce336c;
        _G_0f0d5716ee584deab6b4e7ee1dce336c.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half _G_0f0d5716ee584deab6b4e7ee1dce336c_Axis_1_Float;
        half _G_0f0d5716ee584deab6b4e7ee1dce336c_NegAxis_2_Float;
        SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(half(1), half(0), _G_0f0d5716ee584deab6b4e7ee1dce336c, _G_0f0d5716ee584deab6b4e7ee1dce336c_Axis_1_Float, _G_0f0d5716ee584deab6b4e7ee1dce336c_NegAxis_2_Float);
        float _Subtract_6d93dab28c4d4911be067aa2c15c0c9b_Out_2_Float;
        Unity_Subtract_float(_G_0f0d5716ee584deab6b4e7ee1dce336c_Axis_1_Float, _G_0f0d5716ee584deab6b4e7ee1dce336c_NegAxis_2_Float, _Subtract_6d93dab28c4d4911be067aa2c15c0c9b_Out_2_Float);
        float _Add_018defcefa6942e29deb10b9cf378578_Out_2_Float;
        Unity_Add_float(_Subtract_6d93dab28c4d4911be067aa2c15c0c9b_Out_2_Float, float(1), _Add_018defcefa6942e29deb10b9cf378578_Out_2_Float);
        float _Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float;
        Unity_Multiply_float_float(_Add_018defcefa6942e29deb10b9cf378578_Out_2_Float, 0.5, _Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float);
        float3 _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3);
        float _Split_bbe6128b941e41979d83d67f57627849_R_1_Float = _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3[0];
        float _Split_bbe6128b941e41979d83d67f57627849_G_2_Float = _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3[1];
        float _Split_bbe6128b941e41979d83d67f57627849_B_3_Float = _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3[2];
        float _Split_bbe6128b941e41979d83d67f57627849_A_4_Float = 0;
        float _Absolute_51a90082a9054d91bfca64f7e5297c49_Out_1_Float;
        Unity_Absolute_float(_Split_bbe6128b941e41979d83d67f57627849_G_2_Float, _Absolute_51a90082a9054d91bfca64f7e5297c49_Out_1_Float);
        float _Saturate_ee601b895f834d6ca30749b19481113e_Out_1_Float;
        Unity_Saturate_float(_Absolute_51a90082a9054d91bfca64f7e5297c49_Out_1_Float, _Saturate_ee601b895f834d6ca30749b19481113e_Out_1_Float);
        float _OneMinus_211681ab551d471896d9f1d5252a8bba_Out_1_Float;
        Unity_OneMinus_float(_Saturate_ee601b895f834d6ca30749b19481113e_Out_1_Float, _OneMinus_211681ab551d471896d9f1d5252a8bba_Out_1_Float);
        float _Property_da2c1d123fe54d8a9f31b28775812194_Out_0_Float = _Haze_Strength;
        float _Lerp_1b233567c69f4d6b8f094fe0ba5adeb2_Out_3_Float;
        Unity_Lerp_float(float(12), float(3.5), _Property_da2c1d123fe54d8a9f31b28775812194_Out_0_Float, _Lerp_1b233567c69f4d6b8f094fe0ba5adeb2_Out_3_Float);
        float _Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float;
        Unity_Power_float(_OneMinus_211681ab551d471896d9f1d5252a8bba_Out_1_Float, _Lerp_1b233567c69f4d6b8f094fe0ba5adeb2_Out_3_Float, _Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float);
        float _OneMinus_c541cbd17da24b12b4494c2ce12f97a5_Out_1_Float;
        Unity_OneMinus_float(_Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float, _OneMinus_c541cbd17da24b12b4494c2ce12f97a5_Out_1_Float);
        float _Multiply_1c5f1de5dd5f431eb3c758d3a4b59287_Out_2_Float;
        Unity_Multiply_float_float(0.1, _OneMinus_c541cbd17da24b12b4494c2ce12f97a5_Out_1_Float, _Multiply_1c5f1de5dd5f431eb3c758d3a4b59287_Out_2_Float);
        float _Add_73f28746822a42629e0054eb2bc823ee_Out_2_Float;
        Unity_Add_float(_Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float, _Multiply_1c5f1de5dd5f431eb3c758d3a4b59287_Out_2_Float, _Add_73f28746822a42629e0054eb2bc823ee_Out_2_Float);
        float _Clamp_41f7371ccc544da698ec435f3222fa6e_Out_3_Float;
        Unity_Clamp_float(_Add_73f28746822a42629e0054eb2bc823ee_Out_2_Float, float(0), float(1), _Clamp_41f7371ccc544da698ec435f3222fa6e_Out_3_Float);
        float4 _SampleGradient_f0175d87f8264a2b9039318f0bd895ff_Out_2_Vector4;
        Unity_SampleGradientV1_float(_Property_01e6112c6b2441c5b6e6fdac20b773b1_Out_0_Gradient, _Clamp_41f7371ccc544da698ec435f3222fa6e_Out_3_Float, _SampleGradient_f0175d87f8264a2b9039318f0bd895ff_Out_2_Vector4);
        float3 _Multiply_3508a2d2ba5045379eb5df9463216a71_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_5944a8d7f2834ef6bf339c32906f13dd_Out_2_Vector3, (_SampleGradient_f0175d87f8264a2b9039318f0bd895ff_Out_2_Vector4.xyz), _Multiply_3508a2d2ba5045379eb5df9463216a71_Out_2_Vector3);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_df937e50d90649c3ae85a6152fbecd6d;
        _BottomMargin_df937e50d90649c3ae85a6152fbecd6d.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_df937e50d90649c3ae85a6152fbecd6d_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_df937e50d90649c3ae85a6152fbecd6d, _BottomMargin_df937e50d90649c3ae85a6152fbecd6d_OutVector1_1_Float);
        Bindings_IHHSubGraph_210e4d2b552b95f439d320382950a945_float _IHHSubGraph_916ccb4a130d41bea16700c09fec0520;
        _IHHSubGraph_916ccb4a130d41bea16700c09fec0520.WorldSpacePosition = IN.WorldSpacePosition;
        float _IHHSubGraph_916ccb4a130d41bea16700c09fec0520_OutVector1_1_Float;
        SG_IHHSubGraph_210e4d2b552b95f439d320382950a945_float(_IHHSubGraph_916ccb4a130d41bea16700c09fec0520, _IHHSubGraph_916ccb4a130d41bea16700c09fec0520_OutVector1_1_Float);
        float _Power_3cfbe5a64f9b43c3a940ef678f7fe195_Out_2_Float;
        Unity_Power_float(_IHHSubGraph_916ccb4a130d41bea16700c09fec0520_OutVector1_1_Float, float(1.5), _Power_3cfbe5a64f9b43c3a940ef678f7fe195_Out_2_Float);
        Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float _G_6101fcecd7114972b1efaa6c13168cae;
        _G_6101fcecd7114972b1efaa6c13168cae.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half _G_6101fcecd7114972b1efaa6c13168cae_Axis_1_Float;
        half _G_6101fcecd7114972b1efaa6c13168cae_NegAxis_2_Float;
        SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(half(1), half(0), _G_6101fcecd7114972b1efaa6c13168cae, _G_6101fcecd7114972b1efaa6c13168cae_Axis_1_Float, _G_6101fcecd7114972b1efaa6c13168cae_NegAxis_2_Float);
        float _Add_c489d26d25894e5780a8cec97f2bdec5_Out_2_Float;
        Unity_Add_float(_Power_3cfbe5a64f9b43c3a940ef678f7fe195_Out_2_Float, _G_6101fcecd7114972b1efaa6c13168cae_Axis_1_Float, _Add_c489d26d25894e5780a8cec97f2bdec5_Out_2_Float);
        float _Multiply_1cf4a85661df41d5a8cb6f01b5bcf650_Out_2_Float;
        Unity_Multiply_float_float(_Add_c489d26d25894e5780a8cec97f2bdec5_Out_2_Float, 0.5, _Multiply_1cf4a85661df41d5a8cb6f01b5bcf650_Out_2_Float);
        float _Multiply_d9eb194aaaec4c5e90be162870c3cc89_Out_2_Float;
        Unity_Multiply_float_float(_BottomMargin_df937e50d90649c3ae85a6152fbecd6d_OutVector1_1_Float, _Multiply_1cf4a85661df41d5a8cb6f01b5bcf650_Out_2_Float, _Multiply_d9eb194aaaec4c5e90be162870c3cc89_Out_2_Float);
        float3 _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3);
        float _Split_d5af13543d894489a14f9332ac28c9d1_R_1_Float = _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3[0];
        float _Split_d5af13543d894489a14f9332ac28c9d1_G_2_Float = _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3[1];
        float _Split_d5af13543d894489a14f9332ac28c9d1_B_3_Float = _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3[2];
        float _Split_d5af13543d894489a14f9332ac28c9d1_A_4_Float = 0;
        float _Saturate_7a71970aab60438ca7b7b20d52f1d135_Out_1_Float;
        Unity_Saturate_float(_Split_d5af13543d894489a14f9332ac28c9d1_G_2_Float, _Saturate_7a71970aab60438ca7b7b20d52f1d135_Out_1_Float);
        float _OneMinus_92e84c85e4d9493583d6ec523f4aac6e_Out_1_Float;
        Unity_OneMinus_float(_Saturate_7a71970aab60438ca7b7b20d52f1d135_Out_1_Float, _OneMinus_92e84c85e4d9493583d6ec523f4aac6e_Out_1_Float);
        float _Add_a3994157d05445a090095b25a88446c6_Out_2_Float;
        Unity_Add_float(_OneMinus_92e84c85e4d9493583d6ec523f4aac6e_Out_1_Float, float(0.5), _Add_a3994157d05445a090095b25a88446c6_Out_2_Float);
        float _Multiply_c68f3e26bea14361923f242fdec0048e_Out_2_Float;
        Unity_Multiply_float_float(_Split_d5af13543d894489a14f9332ac28c9d1_G_2_Float, -1, _Multiply_c68f3e26bea14361923f242fdec0048e_Out_2_Float);
        float _Saturate_63a0f6660f2748f6a73d6af628168a5c_Out_1_Float;
        Unity_Saturate_float(_Multiply_c68f3e26bea14361923f242fdec0048e_Out_2_Float, _Saturate_63a0f6660f2748f6a73d6af628168a5c_Out_1_Float);
        float _OneMinus_003371d3b83c48f1ad1d2513343275e9_Out_1_Float;
        Unity_OneMinus_float(_Saturate_63a0f6660f2748f6a73d6af628168a5c_Out_1_Float, _OneMinus_003371d3b83c48f1ad1d2513343275e9_Out_1_Float);
        float _Power_f40ba80da1f347c0bd0f9164db5b000d_Out_2_Float;
        Unity_Power_float(_OneMinus_003371d3b83c48f1ad1d2513343275e9_Out_1_Float, float(10), _Power_f40ba80da1f347c0bd0f9164db5b000d_Out_2_Float);
        float _Multiply_781e862100d24fc0a6c5dccc0b8f6eb6_Out_2_Float;
        Unity_Multiply_float_float(_OneMinus_003371d3b83c48f1ad1d2513343275e9_Out_1_Float, 0, _Multiply_781e862100d24fc0a6c5dccc0b8f6eb6_Out_2_Float);
        float _Add_1edd0ddefdb24161873aa30af48fd4c9_Out_2_Float;
        Unity_Add_float(_Power_f40ba80da1f347c0bd0f9164db5b000d_Out_2_Float, _Multiply_781e862100d24fc0a6c5dccc0b8f6eb6_Out_2_Float, _Add_1edd0ddefdb24161873aa30af48fd4c9_Out_2_Float);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_f2350a746ddc4611aaf144efd7ed6afc;
        _BottomMargin_f2350a746ddc4611aaf144efd7ed6afc.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_f2350a746ddc4611aaf144efd7ed6afc_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_f2350a746ddc4611aaf144efd7ed6afc, _BottomMargin_f2350a746ddc4611aaf144efd7ed6afc_OutVector1_1_Float);
        float _OneMinus_e484be172bd249d88f24864364899ac1_Out_1_Float;
        Unity_OneMinus_float(_BottomMargin_f2350a746ddc4611aaf144efd7ed6afc_OutVector1_1_Float, _OneMinus_e484be172bd249d88f24864364899ac1_Out_1_Float);
        float _Multiply_285173e3363b443eb6af8b6ac02fd1ee_Out_2_Float;
        Unity_Multiply_float_float(_OneMinus_e484be172bd249d88f24864364899ac1_Out_1_Float, 0.4, _Multiply_285173e3363b443eb6af8b6ac02fd1ee_Out_2_Float);
        float _Add_435ab1e14a7e426b818a4757dd947795_Out_2_Float;
        Unity_Add_float(_Add_1edd0ddefdb24161873aa30af48fd4c9_Out_2_Float, _Multiply_285173e3363b443eb6af8b6ac02fd1ee_Out_2_Float, _Add_435ab1e14a7e426b818a4757dd947795_Out_2_Float);
        float _Power_cae5ba0350c04de4b7411ba4e1dd8377_Out_2_Float;
        Unity_Power_float(_Add_435ab1e14a7e426b818a4757dd947795_Out_2_Float, float(2.56), _Power_cae5ba0350c04de4b7411ba4e1dd8377_Out_2_Float);
        float _Minimum_0418f0df67084b5fa98211b3ddfbfac6_Out_2_Float;
        Unity_Minimum_float(_Add_a3994157d05445a090095b25a88446c6_Out_2_Float, _Power_cae5ba0350c04de4b7411ba4e1dd8377_Out_2_Float, _Minimum_0418f0df67084b5fa98211b3ddfbfac6_Out_2_Float);
        float _Add_68666b5746e94f8b904084c8ee551960_Out_2_Float;
        Unity_Add_float(_Multiply_d9eb194aaaec4c5e90be162870c3cc89_Out_2_Float, _Minimum_0418f0df67084b5fa98211b3ddfbfac6_Out_2_Float, _Add_68666b5746e94f8b904084c8ee551960_Out_2_Float);
        float4 _Property_b1198c3ca5d645948d704651058e1065_Out_0_Vector4 = _Sky_Color;
        float4 _Multiply_62d6371e66d74e2a80dd74ca1f4b0e57_Out_2_Vector4;
        Unity_Multiply_float4_float4((_Add_68666b5746e94f8b904084c8ee551960_Out_2_Float.xxxx), _Property_b1198c3ca5d645948d704651058e1065_Out_0_Vector4, _Multiply_62d6371e66d74e2a80dd74ca1f4b0e57_Out_2_Vector4);
        float _Property_b4b5e54fbcc044c1840d135b415c09b6_Out_0_Float = _Light_Pollution;
        float _Multiply_970c06ab37864e5a8910ec8cd3f4bd84_Out_2_Float;
        Unity_Multiply_float_float(_Property_b4b5e54fbcc044c1840d135b415c09b6_Out_0_Float, 0.1, _Multiply_970c06ab37864e5a8910ec8cd3f4bd84_Out_2_Float);
        Bindings_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089;
        _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Direction_1_Vector3;
        float3 _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Color_2_Vector3;
        float _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_ShadowAtten_3_Float;
        SG_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float(half3 (0, 0, 0), false, _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089, _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Direction_1_Vector3, _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Color_2_Vector3, _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_ShadowAtten_3_Float);
        float3 _Saturation_fb51bbc95e0c451bb2ca3bdcfb900b24_Out_2_Vector3;
        Unity_Saturation_float(_GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Color_2_Vector3, float(0), _Saturation_fb51bbc95e0c451bb2ca3bdcfb900b24_Out_2_Vector3);
        float _Float_6edff186ef7940b9bc77f6f47b26aad4_Out_0_Float = float(0);
        float3 _Clamp_8f3f8cad7d3b49e3a06a733a147c5d75_Out_3_Vector3;
        Unity_Clamp_float3(_Saturation_fb51bbc95e0c451bb2ca3bdcfb900b24_Out_2_Vector3, (_Float_6edff186ef7940b9bc77f6f47b26aad4_Out_0_Float.xxx), float3(1, 1, 1), _Clamp_8f3f8cad7d3b49e3a06a733a147c5d75_Out_3_Vector3);
        float _Power_d03a2de7a40b4db2b738c715112a18e8_Out_2_Float;
        Unity_Power_float(_Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float, float(0.8), _Power_d03a2de7a40b4db2b738c715112a18e8_Out_2_Float);
        float _Remap_ce3c7bee85a1457baebf620a9b6b587b_Out_3_Float;
        Unity_Remap_float(_Power_d03a2de7a40b4db2b738c715112a18e8_Out_2_Float, float2 (0.45, 0.6), float2 (0, 1), _Remap_ce3c7bee85a1457baebf620a9b6b587b_Out_3_Float);
        float _Clamp_827779339a274fdc88d3e698ae011970_Out_3_Float;
        Unity_Clamp_float(_Remap_ce3c7bee85a1457baebf620a9b6b587b_Out_3_Float, float(0), float(1), _Clamp_827779339a274fdc88d3e698ae011970_Out_3_Float);
        float3 _Multiply_75735188908a4a57a9be75eff01d3ea3_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Clamp_8f3f8cad7d3b49e3a06a733a147c5d75_Out_3_Vector3, (_Clamp_827779339a274fdc88d3e698ae011970_Out_3_Float.xxx), _Multiply_75735188908a4a57a9be75eff01d3ea3_Out_2_Vector3);
        float3 _Add_5acf440c7f8341eaa017d29d984f3565_Out_2_Vector3;
        Unity_Add_float3((_Multiply_970c06ab37864e5a8910ec8cd3f4bd84_Out_2_Float.xxx), _Multiply_75735188908a4a57a9be75eff01d3ea3_Out_2_Vector3, _Add_5acf440c7f8341eaa017d29d984f3565_Out_2_Vector3);
        float3 _Multiply_e20ad02e76cb494d9711959e7ab14d9b_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Multiply_62d6371e66d74e2a80dd74ca1f4b0e57_Out_2_Vector4.xyz), _Add_5acf440c7f8341eaa017d29d984f3565_Out_2_Vector3, _Multiply_e20ad02e76cb494d9711959e7ab14d9b_Out_2_Vector3);
        float4 _SampleGradient_e6f402b67de946658f3d49f8ef2c26d2_Out_2_Vector4;
        Unity_SampleGradientV1_float(NewGradient(0, 6, 2, float4(1, 1, 1, 0),float4(4, 0.4539363, 0, 0.4699931),float4(2.996078, 0.493884, 0, 0.5000076),float4(2.996078, 1.553205, 0, 0.6),float4(2.118547, 2.118547, 2.118547, 0.7000076),float4(1, 1, 1, 0.8),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float, _SampleGradient_e6f402b67de946658f3d49f8ef2c26d2_Out_2_Vector4);
        float4 _Clamp_edb58ed54f9d4d52803d752ada950ce9_Out_3_Vector4;
        Unity_Clamp_float4(_SampleGradient_e6f402b67de946658f3d49f8ef2c26d2_Out_2_Vector4, float4(0, 0, 0, 0), float4(1, 1, 1, 1), _Clamp_edb58ed54f9d4d52803d752ada950ce9_Out_3_Vector4);
        float3 _Saturation_b2a0668a693c425f8d4c3a925a5eedd1_Out_2_Vector3;
        Unity_Saturation_float((_Clamp_edb58ed54f9d4d52803d752ada950ce9_Out_3_Vector4.xyz), float(0.3), _Saturation_b2a0668a693c425f8d4c3a925a5eedd1_Out_2_Vector3);
        float3 _Multiply_e530e010f9744147b0a688abde27ec68_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_e20ad02e76cb494d9711959e7ab14d9b_Out_2_Vector3, _Saturation_b2a0668a693c425f8d4c3a925a5eedd1_Out_2_Vector3, _Multiply_e530e010f9744147b0a688abde27ec68_Out_2_Vector3);
        float _Float_f5bc9d95c9854659bbc5161a351207e5_Out_0_Float = float(1);
        float4 _Multiply_3e0c4b5d8935412abb84031816ad7c7d_Out_2_Vector4;
        Unity_Multiply_float4_float4(_SampleGradient_f0175d87f8264a2b9039318f0bd895ff_Out_2_Vector4, (_Float_f5bc9d95c9854659bbc5161a351207e5_Out_0_Float.xxxx), _Multiply_3e0c4b5d8935412abb84031816ad7c7d_Out_2_Vector4);
        float3 _Multiply_9729309c7109442fada0a8ff08b56242_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_75735188908a4a57a9be75eff01d3ea3_Out_2_Vector3, (_Multiply_3e0c4b5d8935412abb84031816ad7c7d_Out_2_Vector4.xyz), _Multiply_9729309c7109442fada0a8ff08b56242_Out_2_Vector3);
        float _Float_057d99bc350e40109468e94e4ba84375_Out_0_Float = float(0.2);
        Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float _G_07eb6ef6380a42c78a0936f68a0ab3f1;
        _G_07eb6ef6380a42c78a0936f68a0ab3f1.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half _G_07eb6ef6380a42c78a0936f68a0ab3f1_Axis_1_Float;
        half _G_07eb6ef6380a42c78a0936f68a0ab3f1_NegAxis_2_Float;
        SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(half(1), half(0), _G_07eb6ef6380a42c78a0936f68a0ab3f1, _G_07eb6ef6380a42c78a0936f68a0ab3f1_Axis_1_Float, _G_07eb6ef6380a42c78a0936f68a0ab3f1_NegAxis_2_Float);
        float _Add_289995e9aebf4301b18b6f92e147b346_Out_2_Float;
        Unity_Add_float(_G_07eb6ef6380a42c78a0936f68a0ab3f1_NegAxis_2_Float, float(-0.04), _Add_289995e9aebf4301b18b6f92e147b346_Out_2_Float);
        float _Saturate_7ee7373d114f400faeb08e2cf0d4307c_Out_1_Float;
        Unity_Saturate_float(_Add_289995e9aebf4301b18b6f92e147b346_Out_2_Float, _Saturate_7ee7373d114f400faeb08e2cf0d4307c_Out_1_Float);
        float _Power_a9547f5d3a464e4790e82476762876d4_Out_2_Float;
        Unity_Power_float(_Saturate_7ee7373d114f400faeb08e2cf0d4307c_Out_1_Float, float(0.4), _Power_a9547f5d3a464e4790e82476762876d4_Out_2_Float);
        float _Multiply_df4d258607d44486a11449e64d6852be_Out_2_Float;
        Unity_Multiply_float_float(_Power_a9547f5d3a464e4790e82476762876d4_Out_2_Float, 2, _Multiply_df4d258607d44486a11449e64d6852be_Out_2_Float);
        float _Add_a208a1d796f44559b0a318d613708443_Out_2_Float;
        Unity_Add_float(_Multiply_df4d258607d44486a11449e64d6852be_Out_2_Float, float(-0.3), _Add_a208a1d796f44559b0a318d613708443_Out_2_Float);
        float _Clamp_171cd3e4edf64285a8dac6dbeb8bb44e_Out_3_Float;
        Unity_Clamp_float(_Add_a208a1d796f44559b0a318d613708443_Out_2_Float, float(-0.3), float(0.7), _Clamp_171cd3e4edf64285a8dac6dbeb8bb44e_Out_3_Float);
        float _Float_5f196b71832c4e6d800eb1b79fc566ca_Out_0_Float = _Clamp_171cd3e4edf64285a8dac6dbeb8bb44e_Out_3_Float;
        float _Add_c423554b0b034276a07a9b3c17fdf4f3_Out_2_Float;
        Unity_Add_float(_Float_5f196b71832c4e6d800eb1b79fc566ca_Out_0_Float, float(0.7), _Add_c423554b0b034276a07a9b3c17fdf4f3_Out_2_Float);
        float _OneMinus_5520d3ebf29441d38417bdded937f256_Out_1_Float;
        Unity_OneMinus_float(_Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float, _OneMinus_5520d3ebf29441d38417bdded937f256_Out_1_Float);
        float _Smoothstep_c6865d71edaa45cd9d71e464ef289261_Out_3_Float;
        Unity_Smoothstep_float(_Float_5f196b71832c4e6d800eb1b79fc566ca_Out_0_Float, _Add_c423554b0b034276a07a9b3c17fdf4f3_Out_2_Float, _OneMinus_5520d3ebf29441d38417bdded937f256_Out_1_Float, _Smoothstep_c6865d71edaa45cd9d71e464ef289261_Out_3_Float);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_0a381e7cb297469ab40600952faa97cb;
        _BottomMargin_0a381e7cb297469ab40600952faa97cb.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_0a381e7cb297469ab40600952faa97cb_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_0a381e7cb297469ab40600952faa97cb, _BottomMargin_0a381e7cb297469ab40600952faa97cb_OutVector1_1_Float);
        float _Multiply_04ec0d3c3090428598951334d867a9cf_Out_2_Float;
        Unity_Multiply_float_float(_Smoothstep_c6865d71edaa45cd9d71e464ef289261_Out_3_Float, _BottomMargin_0a381e7cb297469ab40600952faa97cb_OutVector1_1_Float, _Multiply_04ec0d3c3090428598951334d867a9cf_Out_2_Float);
        float _Float_0f1f82d6f8a648dbaa02daeb8bb806d5_Out_0_Float = _Multiply_04ec0d3c3090428598951334d867a9cf_Out_2_Float;
        float _Multiply_28d44dd891754ed9bc34c2f8659cb649_Out_2_Float;
        Unity_Multiply_float_float(_Float_057d99bc350e40109468e94e4ba84375_Out_0_Float, _Float_0f1f82d6f8a648dbaa02daeb8bb806d5_Out_0_Float, _Multiply_28d44dd891754ed9bc34c2f8659cb649_Out_2_Float);
        Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float _G_df7eb56be6394e2ba4b1c697d988796a;
        _G_df7eb56be6394e2ba4b1c697d988796a.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half _G_df7eb56be6394e2ba4b1c697d988796a_Axis_1_Float;
        half _G_df7eb56be6394e2ba4b1c697d988796a_NegAxis_2_Float;
        SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(half(1), half(0), _G_df7eb56be6394e2ba4b1c697d988796a, _G_df7eb56be6394e2ba4b1c697d988796a_Axis_1_Float, _G_df7eb56be6394e2ba4b1c697d988796a_NegAxis_2_Float);
        float _OneMinus_40f689075b014f20a003f776e474ed41_Out_1_Float;
        Unity_OneMinus_float(_G_df7eb56be6394e2ba4b1c697d988796a_Axis_1_Float, _OneMinus_40f689075b014f20a003f776e474ed41_Out_1_Float);
        float _Power_ce04259387f04dcc8e6ce87bcbfcd9ba_Out_2_Float;
        Unity_Power_float(_OneMinus_40f689075b014f20a003f776e474ed41_Out_1_Float, float(0.1), _Power_ce04259387f04dcc8e6ce87bcbfcd9ba_Out_2_Float);
        Bindings_IHHSubGraph_210e4d2b552b95f439d320382950a945_float _IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90;
        _IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90.WorldSpacePosition = IN.WorldSpacePosition;
        float _IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90_OutVector1_1_Float;
        SG_IHHSubGraph_210e4d2b552b95f439d320382950a945_float(_IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90, _IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90_OutVector1_1_Float);
        float _Power_1cfcdb48c95d4629949b15d90df2533c_Out_2_Float;
        Unity_Power_float(_IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90_OutVector1_1_Float, float(1.5), _Power_1cfcdb48c95d4629949b15d90df2533c_Out_2_Float);
        float _Power_ba6165353f64456a8c9387bd3ff533d0_Out_2_Float;
        Unity_Power_float(_G_df7eb56be6394e2ba4b1c697d988796a_Axis_1_Float, float(1.5), _Power_ba6165353f64456a8c9387bd3ff533d0_Out_2_Float);
        float _Multiply_2126d490984d4be5bdfe31394daaefeb_Out_2_Float;
        Unity_Multiply_float_float(_Power_ba6165353f64456a8c9387bd3ff533d0_Out_2_Float, 2, _Multiply_2126d490984d4be5bdfe31394daaefeb_Out_2_Float);
        float _Maximum_f0cea5402e81447b9b74e49db3d5a701_Out_2_Float;
        Unity_Maximum_float(_Power_1cfcdb48c95d4629949b15d90df2533c_Out_2_Float, _Multiply_2126d490984d4be5bdfe31394daaefeb_Out_2_Float, _Maximum_f0cea5402e81447b9b74e49db3d5a701_Out_2_Float);
        float _Multiply_a753e72afb5942da802fbaa508147fa8_Out_2_Float;
        Unity_Multiply_float_float(_Power_ce04259387f04dcc8e6ce87bcbfcd9ba_Out_2_Float, _Maximum_f0cea5402e81447b9b74e49db3d5a701_Out_2_Float, _Multiply_a753e72afb5942da802fbaa508147fa8_Out_2_Float);
        float _Lerp_6f838ef66c1648038b30440bc8ef7617_Out_3_Float;
        Unity_Lerp_float(_Multiply_28d44dd891754ed9bc34c2f8659cb649_Out_2_Float, _Float_0f1f82d6f8a648dbaa02daeb8bb806d5_Out_0_Float, _Multiply_a753e72afb5942da802fbaa508147fa8_Out_2_Float, _Lerp_6f838ef66c1648038b30440bc8ef7617_Out_3_Float);
        float _Multiply_f0f7f5ae35874162b2cbf9857300afc2_Out_2_Float;
        Unity_Multiply_float_float(_Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float, _Lerp_6f838ef66c1648038b30440bc8ef7617_Out_3_Float, _Multiply_f0f7f5ae35874162b2cbf9857300afc2_Out_2_Float);
        float _Multiply_a844ec42b5c94c0e8d8e0dbe805be790_Out_2_Float;
        Unity_Multiply_float_float(_Clamp_827779339a274fdc88d3e698ae011970_Out_3_Float, _Multiply_f0f7f5ae35874162b2cbf9857300afc2_Out_2_Float, _Multiply_a844ec42b5c94c0e8d8e0dbe805be790_Out_2_Float);
        float3 _Lerp_c4d456471c744b49b9faab47a4fc5bd1_Out_3_Vector3;
        Unity_Lerp_float3(_Multiply_e530e010f9744147b0a688abde27ec68_Out_2_Vector3, _Multiply_9729309c7109442fada0a8ff08b56242_Out_2_Vector3, (_Multiply_a844ec42b5c94c0e8d8e0dbe805be790_Out_2_Float.xxx), _Lerp_c4d456471c744b49b9faab47a4fc5bd1_Out_3_Vector3);
        float3 _Add_d8a7191925d1427185d1064714fd74d2_Out_2_Vector3;
        Unity_Add_float3(_Multiply_3508a2d2ba5045379eb5df9463216a71_Out_2_Vector3, _Lerp_c4d456471c744b49b9faab47a4fc5bd1_Out_3_Vector3, _Add_d8a7191925d1427185d1064714fd74d2_Out_2_Vector3);
        float _Property_4febc69456f24ff594aaff18cc78e70a_Out_0_Float = _Exposure;
        float3 _Multiply_4abce12d04e6472ebd0379bfaec50e20_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Add_d8a7191925d1427185d1064714fd74d2_Out_2_Vector3, (_Property_4febc69456f24ff594aaff18cc78e70a_Out_0_Float.xxx), _Multiply_4abce12d04e6472ebd0379bfaec50e20_Out_2_Vector3);
        UnityTextureCube _Property_7a236dc7b986418295c055925dd74f56_Out_0_Cubemap = _Starmap;
        float3 _Normalize_c60bad13e530400e9acdf25d9a102cee_Out_1_Vector3;
        Unity_Normalize_float3(IN.ObjectSpaceNormal, _Normalize_c60bad13e530400e9acdf25d9a102cee_Out_1_Vector3);
        float _Property_58bbd1cfb63e4b3e93aa7d9537e93919_Out_0_Float = _Star_Roration_Speed;
        float _Multiply_caf37e3f96fd454592ee1571efd643e2_Out_2_Float;
        Unity_Multiply_float_float(IN.TimeParameters.x, _Property_58bbd1cfb63e4b3e93aa7d9537e93919_Out_0_Float, _Multiply_caf37e3f96fd454592ee1571efd643e2_Out_2_Float);
        float3 _RotateAboutAxis_18feb3536cc341de8dccef57e7646c24_Out_3_Vector3;
        Unity_Rotate_About_Axis_Degrees_float(_Normalize_c60bad13e530400e9acdf25d9a102cee_Out_1_Vector3, float3 (1, 0, 0), _Multiply_caf37e3f96fd454592ee1571efd643e2_Out_2_Float, _RotateAboutAxis_18feb3536cc341de8dccef57e7646c24_Out_3_Vector3);
        float4 _SampleReflectedCubemap_1f499bf7f9484e52a24768d66256410e_Out_0_Vector4 = SAMPLE_TEXTURECUBE_LOD(_Property_7a236dc7b986418295c055925dd74f56_Out_0_Cubemap.tex, _Property_7a236dc7b986418295c055925dd74f56_Out_0_Cubemap.samplerstate, reflect(-IN.ObjectSpaceViewDirection, _RotateAboutAxis_18feb3536cc341de8dccef57e7646c24_Out_3_Vector3), float(0));
        float _Float_013baa7b5c7c409880b99e1afe6bffcf_Out_0_Float = float(6);
        float4 _Power_a1762f9654be457497b776e3eaabec5b_Out_2_Vector4;
        Unity_Power_float4(_SampleReflectedCubemap_1f499bf7f9484e52a24768d66256410e_Out_0_Vector4, (_Float_013baa7b5c7c409880b99e1afe6bffcf_Out_0_Float.xxxx), _Power_a1762f9654be457497b776e3eaabec5b_Out_2_Vector4);
        float _Float_e04f0f45684146e8a99d28233468eabc_Out_0_Float = float(0.5);
        float _Property_9b2c2d287d6444ab952da3ebe896b4c3_Out_0_Float = _Light_Pollution;
        float _Multiply_9c3dee328c674b5db172bd8114acb8b0_Out_2_Float;
        Unity_Multiply_float_float(_Property_9b2c2d287d6444ab952da3ebe896b4c3_Out_0_Float, 1.1, _Multiply_9c3dee328c674b5db172bd8114acb8b0_Out_2_Float);
        float _Clamp_a645348a153844c4a52147e7c49148ef_Out_3_Float;
        Unity_Clamp_float(_Multiply_9c3dee328c674b5db172bd8114acb8b0_Out_2_Float, float(0), float(1), _Clamp_a645348a153844c4a52147e7c49148ef_Out_3_Float);
        float _Lerp_b45be9d3aa3f456ebfd4109ddd645f32_Out_3_Float;
        Unity_Lerp_float(_Float_e04f0f45684146e8a99d28233468eabc_Out_0_Float, float(0), _Clamp_a645348a153844c4a52147e7c49148ef_Out_3_Float, _Lerp_b45be9d3aa3f456ebfd4109ddd645f32_Out_3_Float);
        float4 _Multiply_735b06ecba954052b597d2070edcc61c_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Power_a1762f9654be457497b776e3eaabec5b_Out_2_Vector4, (_Lerp_b45be9d3aa3f456ebfd4109ddd645f32_Out_3_Float.xxxx), _Multiply_735b06ecba954052b597d2070edcc61c_Out_2_Vector4);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_64f1341362d54501b99283e28ee96c24;
        _BottomMargin_64f1341362d54501b99283e28ee96c24.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_64f1341362d54501b99283e28ee96c24_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_64f1341362d54501b99283e28ee96c24, _BottomMargin_64f1341362d54501b99283e28ee96c24_OutVector1_1_Float);
        float4 _Multiply_d408cb06455c4cb398283062217ba4e2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Multiply_735b06ecba954052b597d2070edcc61c_Out_2_Vector4, (_BottomMargin_64f1341362d54501b99283e28ee96c24_OutVector1_1_Float.xxxx), _Multiply_d408cb06455c4cb398283062217ba4e2_Out_2_Vector4);
        float3 _Maximum_2223a2cd9f6d48c9aa35ffdcd7090a87_Out_2_Vector3;
        Unity_Maximum_float3(_Multiply_4abce12d04e6472ebd0379bfaec50e20_Out_2_Vector3, (_Multiply_d408cb06455c4cb398283062217ba4e2_Out_2_Vector4.xyz), _Maximum_2223a2cd9f6d48c9aa35ffdcd7090a87_Out_2_Vector3);
        float4 _Property_71ee008a367d4150a8405aecd69e129a_Out_0_Vector4 = _Tint;
        float3 _Multiply_fbeeb451a4a341c7bf1aacad6cf2c1c8_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Maximum_2223a2cd9f6d48c9aa35ffdcd7090a87_Out_2_Vector3, (_Property_71ee008a367d4150a8405aecd69e129a_Out_0_Vector4.xyz), _Multiply_fbeeb451a4a341c7bf1aacad6cf2c1c8_Out_2_Vector3);
        OutVector3_1 = _Multiply_fbeeb451a4a341c7bf1aacad6cf2c1c8_Out_2_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTextureCube _Property_c4b1f102431c4de9b650909cd1666eb0_Out_0_Cubemap = UnityBuildTextureCubeStruct(_Starmap);
            float4 _Property_99b0b36085404ae78f8dcec7c5adcc1c_Out_0_Vector4 = _Sky_Color;
            float _Property_b604e6b76ec64065a61f579da44433c9_Out_0_Float = _Exposure;
            float4 _Property_09dc65a7df1b45219455f00f66e56201_Out_0_Vector4 = _Tint;
            float _Property_5cc7dd99c20d48babd3d3eed6c386df3_Out_0_Float = _Sun_Size;
            float _Property_4f3eb11bfbea4f0e9efafe3a80cb295b_Out_0_Float = _Light_Pollution;
            float _Property_6c39e23659804293b9afb889c2db97f4_Out_0_Float = _Haze_Strength;
            float _Property_c1f9f79b31f74124b57941edfa55c012_Out_0_Float = _Star_Roration_Speed;
            Bindings_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float _Sky_ce6521cb02ac4bddbc56ea4fda912eaa;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.ObjectSpaceNormal = IN.ObjectSpaceNormal;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.WorldSpacePosition = IN.WorldSpacePosition;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.TimeParameters = IN.TimeParameters;
            float3 _Sky_ce6521cb02ac4bddbc56ea4fda912eaa_OutVector3_1_Vector3;
            SG_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float(_Property_c4b1f102431c4de9b650909cd1666eb0_Out_0_Cubemap, _Property_99b0b36085404ae78f8dcec7c5adcc1c_Out_0_Vector4, _Property_b604e6b76ec64065a61f579da44433c9_Out_0_Float, _Property_09dc65a7df1b45219455f00f66e56201_Out_0_Vector4, _Property_5cc7dd99c20d48babd3d3eed6c386df3_Out_0_Float, float(0), _Property_4f3eb11bfbea4f0e9efafe3a80cb295b_Out_0_Float, _Property_6c39e23659804293b9afb889c2db97f4_Out_0_Float, NewGradient(0, 6, 2, float4(1, 1, 1, 0),float4(16, 0.5333333, 0, 0.4699931),float4(2.996078, 0.493884, 0, 0.5000076),float4(2.996078, 1.553205, 0, 0.6),float4(2.118547, 2.118547, 2.118547, 0.7000076),float4(1, 1, 1, 0.8),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _Property_c1f9f79b31f74124b57941edfa55c012_Out_0_Float, _Sky_ce6521cb02ac4bddbc56ea4fda912eaa, _Sky_ce6521cb02ac4bddbc56ea4fda912eaa_OutVector3_1_Vector3);
            surface.BaseColor = _Sky_ce6521cb02ac4bddbc56ea4fda912eaa_OutVector3_1_Vector3;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
            output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
        
        
            output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
            output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
        
        // Render State
        Cull Back
        ZTest LEqual
        ZWrite On
        ColorMask R
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Sky_Color;
        float _Exposure;
        float4 _Tint;
        float _Sun_Size;
        float _Haze_Strength;
        float _Light_Pollution;
        float _Star_Roration_Speed;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURECUBE(_Starmap);
        SAMPLER(sampler_Starmap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthNormalsOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }
        
        // Render State
        Cull Back
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define VARYINGS_NEED_NORMAL_WS
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Sky_Color;
        float _Exposure;
        float4 _Tint;
        float _Sun_Size;
        float _Haze_Strength;
        float _Light_Pollution;
        float _Star_Roration_Speed;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURECUBE(_Starmap);
        SAMPLER(sampler_Starmap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
        // Render State
        Cull Back
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define VARYINGS_NEED_NORMAL_WS
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Sky_Color;
        float _Exposure;
        float4 _Tint;
        float _Sun_Size;
        float _Haze_Strength;
        float _Light_Pollution;
        float _Star_Roration_Speed;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURECUBE(_Starmap);
        SAMPLER(sampler_Starmap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }
        
        // Render State
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_GBUFFER
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceViewDirection;
             float3 WorldSpaceViewDirection;
             float3 WorldSpacePosition;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP0;
            #endif
             float3 positionWS : INTERP1;
             float3 normalWS : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Sky_Color;
        float _Exposure;
        float4 _Tint;
        float _Sun_Size;
        float _Haze_Strength;
        float _Light_Pollution;
        float _Star_Roration_Speed;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURECUBE(_Starmap);
        SAMPLER(sampler_Starmap);
        
        // Graph Includes
        #include "Assets/WG Free Skies/Skyboxes/URP Procedural Skybox/CustomFunctions/GetMainLight.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void MainLightDirection_float(out float3 Direction)
        {
            #if SHADERGRAPH_PREVIEW
            Direction = half3(-0.5, -0.5, 0);
            #else
            Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
            #endif
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float(Bindings_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float IN, out float OutVector1_1)
        {
        float3 _MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3);
        float3 _Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3);
        float3 _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3, float3(-1, -1, -1), _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3);
        float _DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float;
        Unity_DotProduct_float3(_MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3, _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3, _DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float);
        float _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float;
        Unity_Saturate_float(_DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float, _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float);
        OutVector1_1 = _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        struct Bindings_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float(float3 _WorldPos, bool _WorldPos_a8754bc66eea418fb6c8b3ac5ee821e6_IsConnected, Bindings_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float IN, out float3 Direction_1, out float3 Color_2, out float ShadowAtten_3)
        {
        float3 _Property_da55b32ae35a4271a77c0649c9147c66_Out_0_Vector3 = _WorldPos;
        bool _Property_da55b32ae35a4271a77c0649c9147c66_Out_0_Vector3_IsConnected = _WorldPos_a8754bc66eea418fb6c8b3ac5ee821e6_IsConnected;
        float3 _BranchOnInputConnection_2e680a118c3a482aa321329faf04e029_Out_3_Vector3 = _Property_da55b32ae35a4271a77c0649c9147c66_Out_0_Vector3_IsConnected ? _Property_da55b32ae35a4271a77c0649c9147c66_Out_0_Vector3 : IN.WorldSpacePosition;
        float3 _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Direction_2_Vector3;
        float3 _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Color_1_Vector3;
        float _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_ShadowAtten_3_Float;
        GetMainLight_float(_BranchOnInputConnection_2e680a118c3a482aa321329faf04e029_Out_3_Vector3, _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Direction_2_Vector3, _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Color_1_Vector3, _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_ShadowAtten_3_Float);
        Direction_1 = _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Direction_2_Vector3;
        Color_2 = _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_Color_1_Vector3;
        ShadowAtten_3 = _GetMainLightCustomFunction_5452f3ed14be48c7b74cc8b101134fce_ShadowAtten_3_Float;
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        void Unity_Maximum_float3(float3 A, float3 B, out float3 Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Ceiling_float3(float3 In, out float3 Out)
        {
            Out = ceil(In);
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        struct Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float IN, out float OutVector1_1)
        {
        float3 _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3);
        float _Split_3f1176004e8b43af98a33a196b09074f_R_1_Float = _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3[0];
        float _Split_3f1176004e8b43af98a33a196b09074f_G_2_Float = _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3[1];
        float _Split_3f1176004e8b43af98a33a196b09074f_B_3_Float = _Normalize_142230b9dd9c456c8d99fcb0156875c7_Out_1_Vector3[2];
        float _Split_3f1176004e8b43af98a33a196b09074f_A_4_Float = 0;
        float _Multiply_78434ea90a094b52afd20ccb59993614_Out_2_Float;
        Unity_Multiply_float_float(_Split_3f1176004e8b43af98a33a196b09074f_G_2_Float, -1, _Multiply_78434ea90a094b52afd20ccb59993614_Out_2_Float);
        float _Saturate_a5d3f27860cb43829ec8334a9c7621a9_Out_1_Float;
        Unity_Saturate_float(_Multiply_78434ea90a094b52afd20ccb59993614_Out_2_Float, _Saturate_a5d3f27860cb43829ec8334a9c7621a9_Out_1_Float);
        float _OneMinus_5c142230ac4c453793d389e55063c90b_Out_1_Float;
        Unity_OneMinus_float(_Saturate_a5d3f27860cb43829ec8334a9c7621a9_Out_1_Float, _OneMinus_5c142230ac4c453793d389e55063c90b_Out_1_Float);
        float _Step_468d240d2ff2400d8c04b71c15e4a886_Out_2_Float;
        Unity_Step_float(float(1), _OneMinus_5c142230ac4c453793d389e55063c90b_Out_1_Float, _Step_468d240d2ff2400d8c04b71c15e4a886_Out_2_Float);
        float _Saturate_e1b2e9e85e434bdfb40a8be726cfbdb4_Out_1_Float;
        Unity_Saturate_float(_Step_468d240d2ff2400d8c04b71c15e4a886_Out_2_Float, _Saturate_e1b2e9e85e434bdfb40a8be726cfbdb4_Out_1_Float);
        OutVector1_1 = _Saturate_e1b2e9e85e434bdfb40a8be726cfbdb4_Out_1_Float;
        }
        
        void Unity_Rotate_About_Axis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
        {
            Rotation = radians(Rotation);
        
            float s = sin(Rotation);
            float c = cos(Rotation);
            float one_minus_c = 1.0 - c;
        
            Axis = normalize(Axis);
        
            float3x3 rot_mat = { one_minus_c * Axis.x * Axis.x + c,            one_minus_c * Axis.x * Axis.y - Axis.z * s,     one_minus_c * Axis.z * Axis.x + Axis.y * s,
                                      one_minus_c * Axis.x * Axis.y + Axis.z * s,   one_minus_c * Axis.y * Axis.y + c,              one_minus_c * Axis.y * Axis.z - Axis.x * s,
                                      one_minus_c * Axis.z * Axis.x - Axis.y * s,   one_minus_c * Axis.y * Axis.z + Axis.x * s,     one_minus_c * Axis.z * Axis.z + c
                                    };
        
            Out = mul(rot_mat,  In);
        }
        
        void Unity_CrossProduct_float(float3 A, float3 B, out float3 Out)
        {
            Out = cross(A, B);
        }
        
        struct Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float
        {
        float3 WorldSpaceViewDirection;
        };
        
        void SG_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float(float3 _Axis, float _Angle, float _LightAngle, float3 _Direction_Mask, float3 _Reference_Direction, float _Rotation, Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float IN, out float3 Out_1, out float3 Mask_2, out float3 Reference_Direction_3)
        {
        float3 _MainLightDirection_3f978ad70c274d23b6d8e1b2cc0f9599_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_3f978ad70c274d23b6d8e1b2cc0f9599_Direction_0_Vector3);
        float _Property_11f9a030df9f44aa84e5f7b3c2c61320_Out_0_Float = _Rotation;
        float _Multiply_ce46e0dddd1044f09ef7f44bedb27b67_Out_2_Float;
        Unity_Multiply_float_float(_Property_11f9a030df9f44aa84e5f7b3c2c61320_Out_0_Float, 2, _Multiply_ce46e0dddd1044f09ef7f44bedb27b67_Out_2_Float);
        float3 _RotateAboutAxis_dc58e436703348b59b729c00aa426b02_Out_3_Vector3;
        Unity_Rotate_About_Axis_Degrees_float(_MainLightDirection_3f978ad70c274d23b6d8e1b2cc0f9599_Direction_0_Vector3, float3 (0, -1, 0), _Multiply_ce46e0dddd1044f09ef7f44bedb27b67_Out_2_Float, _RotateAboutAxis_dc58e436703348b59b729c00aa426b02_Out_3_Vector3);
        float3 _Property_7bfa7986748e4f53adfa9a12c3d3fe12_Out_0_Vector3 = _Axis;
        float _Property_5ea44cf579454cb781eb086b6d3be43b_Out_0_Float = _LightAngle;
        float3 _RotateAboutAxis_73980326b96a4b3e9824340784e77c96_Out_3_Vector3;
        Unity_Rotate_About_Axis_Degrees_float(_RotateAboutAxis_dc58e436703348b59b729c00aa426b02_Out_3_Vector3, _Property_7bfa7986748e4f53adfa9a12c3d3fe12_Out_0_Vector3, _Property_5ea44cf579454cb781eb086b6d3be43b_Out_0_Float, _RotateAboutAxis_73980326b96a4b3e9824340784e77c96_Out_3_Vector3);
        float _Property_7402c98ce263489f9c6d8d8b19a42690_Out_0_Float = _Angle;
        float3 _RotateAboutAxis_20f73daa95264608b2a0ebff411dacd7_Out_3_Vector3;
        Unity_Rotate_About_Axis_Degrees_float(IN.WorldSpaceViewDirection, _Property_7bfa7986748e4f53adfa9a12c3d3fe12_Out_0_Vector3, _Property_7402c98ce263489f9c6d8d8b19a42690_Out_0_Float, _RotateAboutAxis_20f73daa95264608b2a0ebff411dacd7_Out_3_Vector3);
        float3 _Property_c17dbfea91114909b928e649fc67f177_Out_0_Vector3 = _Direction_Mask;
        float3 _Multiply_7f9de497dee84d6ca47e9e50c5b283cb_Out_2_Vector3;
        Unity_Multiply_float3_float3(_RotateAboutAxis_20f73daa95264608b2a0ebff411dacd7_Out_3_Vector3, _Property_c17dbfea91114909b928e649fc67f177_Out_0_Vector3, _Multiply_7f9de497dee84d6ca47e9e50c5b283cb_Out_2_Vector3);
        float3 _CrossProduct_c8cb8c20650a480f9f3a9ecfb823790f_Out_2_Vector3;
        Unity_CrossProduct_float(_RotateAboutAxis_73980326b96a4b3e9824340784e77c96_Out_3_Vector3, _Multiply_7f9de497dee84d6ca47e9e50c5b283cb_Out_2_Vector3, _CrossProduct_c8cb8c20650a480f9f3a9ecfb823790f_Out_2_Vector3);
        float3 _Property_f6aa6b80c0d143758a949788b3b91333_Out_0_Vector3 = _Reference_Direction;
        float3 _Multiply_118a95527ea74b319266cf0312071080_Out_2_Vector3;
        Unity_Multiply_float3_float3(IN.WorldSpaceViewDirection, _Property_f6aa6b80c0d143758a949788b3b91333_Out_0_Vector3, _Multiply_118a95527ea74b319266cf0312071080_Out_2_Vector3);
        float3 _Multiply_d1ec4933ebc14085afd38f962b90902f_Out_2_Vector3;
        Unity_Multiply_float3_float3(_CrossProduct_c8cb8c20650a480f9f3a9ecfb823790f_Out_2_Vector3, _Multiply_118a95527ea74b319266cf0312071080_Out_2_Vector3, _Multiply_d1ec4933ebc14085afd38f962b90902f_Out_2_Vector3);
        Out_1 = _Multiply_d1ec4933ebc14085afd38f962b90902f_Out_2_Vector3;
        Mask_2 = _CrossProduct_c8cb8c20650a480f9f3a9ecfb823790f_Out_2_Vector3;
        Reference_Direction_3 = _Multiply_118a95527ea74b319266cf0312071080_Out_2_Vector3;
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        struct Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float
        {
        };
        
        void SG_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float(float _Single_Channel, Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float IN, out float Mask_Integral_1, out float Axis_2, out float Neg_Axis_3)
        {
        float _Property_021e4e3a38294a489e1ff0f55bd1c796_Out_0_Float = _Single_Channel;
        float _Clamp_d47adc1041f743a9806830337d36b762_Out_3_Float;
        Unity_Clamp_float(_Property_021e4e3a38294a489e1ff0f55bd1c796_Out_0_Float, float(0), float(1), _Clamp_d47adc1041f743a9806830337d36b762_Out_3_Float);
        float _Remap_defdc492c647436088c64df42e34f072_Out_3_Float;
        Unity_Remap_float(_Property_021e4e3a38294a489e1ff0f55bd1c796_Out_0_Float, float2 (-1, 1), float2 (1, -1), _Remap_defdc492c647436088c64df42e34f072_Out_3_Float);
        float _Clamp_02106530d3464895a9f4c87814088e67_Out_3_Float;
        Unity_Clamp_float(_Remap_defdc492c647436088c64df42e34f072_Out_3_Float, float(0), float(1), _Clamp_02106530d3464895a9f4c87814088e67_Out_3_Float);
        float _Add_2af9063bdac24d59a5077f7797ab02eb_Out_2_Float;
        Unity_Add_float(_Clamp_d47adc1041f743a9806830337d36b762_Out_3_Float, _Clamp_02106530d3464895a9f4c87814088e67_Out_3_Float, _Add_2af9063bdac24d59a5077f7797ab02eb_Out_2_Float);
        Mask_Integral_1 = _Add_2af9063bdac24d59a5077f7797ab02eb_Out_2_Float;
        Axis_2 = _Clamp_d47adc1041f743a9806830337d36b762_Out_3_Float;
        Neg_Axis_3 = _Clamp_02106530d3464895a9f4c87814088e67_Out_3_Float;
        }
        
        struct Bindings_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float
        {
        };
        
        void SG_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float(float _F1, float _F2, float _F3, float _Clamp, Bindings_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float IN, out float Out_1)
        {
        float _Property_bc6007b810004657a6743d108cb7122a_Out_0_Float = _F1;
        float _Property_60a7b711175b4f5093b54107b450564c_Out_0_Float = _F2;
        float _Add_a2f39a5f48354341992976d8d319eae1_Out_2_Float;
        Unity_Add_float(_Property_bc6007b810004657a6743d108cb7122a_Out_0_Float, _Property_60a7b711175b4f5093b54107b450564c_Out_0_Float, _Add_a2f39a5f48354341992976d8d319eae1_Out_2_Float);
        float _Property_0c2b6cf8d0834b34b999acbbb5028f14_Out_0_Float = _F3;
        float _Add_e1fde91e48f243bfba8cb8909aefedcf_Out_2_Float;
        Unity_Add_float(_Add_a2f39a5f48354341992976d8d319eae1_Out_2_Float, _Property_0c2b6cf8d0834b34b999acbbb5028f14_Out_0_Float, _Add_e1fde91e48f243bfba8cb8909aefedcf_Out_2_Float);
        Out_1 = _Add_e1fde91e48f243bfba8cb8909aefedcf_Out_2_Float;
        }
        
        struct Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float
        {
        float3 WorldSpaceViewDirection;
        };
        
        void SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(float _Power, float _Rotation, Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float IN, out float Axis_1, out float Neg_Axis_2)
        {
        float3 _Vector3_558649f6bb6047ecae9676e2bba7c29a_Out_0_Vector3 = float3(float(0), float(1), float(0));
        float _Float_e6cd52f775e94a2c8a3c67562f1d650f_Out_0_Float = float(90);
        float3 _Vector3_5c17e80b8dff472b9e2517353541e86e_Out_0_Vector3 = float3(float(0), float(0), float(1));
        float3 _Vector3_5df26d2e743848f5aabbcaf4ede22863_Out_0_Vector3 = float3(float(1), float(0), float(0));
        float _Property_8b9491b137964a47b72d0c659f60a15c_Out_0_Float = _Rotation;
        Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5;
        _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3;
        half3 _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Mask_2_Vector3;
        half3 _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_ReferenceDirection_3_Vector3;
        SG_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float(_Vector3_558649f6bb6047ecae9676e2bba7c29a_Out_0_Vector3, _Float_e6cd52f775e94a2c8a3c67562f1d650f_Out_0_Float, _Float_e6cd52f775e94a2c8a3c67562f1d650f_Out_0_Float, _Vector3_5c17e80b8dff472b9e2517353541e86e_Out_0_Vector3, _Vector3_5df26d2e743848f5aabbcaf4ede22863_Out_0_Vector3, _Property_8b9491b137964a47b72d0c659f60a15c_Out_0_Float, _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5, _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3, _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Mask_2_Vector3, _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_ReferenceDirection_3_Vector3);
        float _Split_89a273ee7bcd4b6ebede46ccbe2d595e_R_1_Float = _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3[0];
        float _Split_89a273ee7bcd4b6ebede46ccbe2d595e_G_2_Float = _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3[1];
        float _Split_89a273ee7bcd4b6ebede46ccbe2d595e_B_3_Float = _DirectionMask_a0aaf29fa7ea4122bdf3eebfa53b20c5_Out_1_Vector3[2];
        float _Split_89a273ee7bcd4b6ebede46ccbe2d595e_A_4_Float = 0;
        Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9;
        half _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_MaskIntegral_1_Float;
        half _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_Axis_2_Float;
        half _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_NegAxis_3_Float;
        SG_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float(_Split_89a273ee7bcd4b6ebede46ccbe2d595e_R_1_Float, _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9, _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_MaskIntegral_1_Float, _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_Axis_2_Float, _MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_NegAxis_3_Float);
        float3 _Vector3_e94d1720a60b4038aab5b0365a843891_Out_0_Vector3 = float3(float(0), float(1), float(0));
        float _Float_1777c8b349984742adc51c84c3cb32c9_Out_0_Float = float(90);
        float3 _Vector3_ad840f874e2649f28c7082718003a36d_Out_0_Vector3 = float3(float(1), float(0), float(0));
        float3 _Vector3_dc33e0531d0d459ca8bf02b7a2cf408e_Out_0_Vector3 = float3(float(0), float(0), float(1));
        Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d;
        _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3;
        half3 _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Mask_2_Vector3;
        half3 _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_ReferenceDirection_3_Vector3;
        SG_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float(_Vector3_e94d1720a60b4038aab5b0365a843891_Out_0_Vector3, _Float_1777c8b349984742adc51c84c3cb32c9_Out_0_Float, _Float_1777c8b349984742adc51c84c3cb32c9_Out_0_Float, _Vector3_ad840f874e2649f28c7082718003a36d_Out_0_Vector3, _Vector3_dc33e0531d0d459ca8bf02b7a2cf408e_Out_0_Vector3, _Property_8b9491b137964a47b72d0c659f60a15c_Out_0_Float, _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d, _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3, _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Mask_2_Vector3, _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_ReferenceDirection_3_Vector3);
        float _Split_1d631d3e539647e6b433a95122d49fb4_R_1_Float = _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3[0];
        float _Split_1d631d3e539647e6b433a95122d49fb4_G_2_Float = _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3[1];
        float _Split_1d631d3e539647e6b433a95122d49fb4_B_3_Float = _DirectionMask_4d42f12b498f441bbd7d3c92c09a871d_Out_1_Vector3[2];
        float _Split_1d631d3e539647e6b433a95122d49fb4_A_4_Float = 0;
        Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40;
        half _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_MaskIntegral_1_Float;
        half _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_Axis_2_Float;
        half _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_NegAxis_3_Float;
        SG_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float(_Split_1d631d3e539647e6b433a95122d49fb4_B_3_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_MaskIntegral_1_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_Axis_2_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_NegAxis_3_Float);
        float3 _Vector3_1f34ecf82e914ca5bcee1bef0b9ef3fe_Out_0_Vector3 = float3(float(1), float(0), float(0));
        float _Float_ddd7e4715cf94f0aa27ff5b5e9cf660f_Out_0_Float = float(90);
        float3 _Vector3_431fd60e4b184036b6b6f257448fff42_Out_0_Vector3 = float3(float(0), float(0), float(1));
        float3 _Vector3_de948620452742bfb19d3eb02856f4a7_Out_0_Vector3 = float3(float(0), float(-1), float(0));
        Bindings_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float _DirectionMask_e7bee712463546a09117207b42c4d099;
        _DirectionMask_e7bee712463546a09117207b42c4d099.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _DirectionMask_e7bee712463546a09117207b42c4d099_Out_1_Vector3;
        half3 _DirectionMask_e7bee712463546a09117207b42c4d099_Mask_2_Vector3;
        half3 _DirectionMask_e7bee712463546a09117207b42c4d099_ReferenceDirection_3_Vector3;
        SG_DirectionMask_7ecd397391926b54ba8ab06e88be5324_float(_Vector3_1f34ecf82e914ca5bcee1bef0b9ef3fe_Out_0_Vector3, _Float_ddd7e4715cf94f0aa27ff5b5e9cf660f_Out_0_Float, half(0), _Vector3_431fd60e4b184036b6b6f257448fff42_Out_0_Vector3, _Vector3_de948620452742bfb19d3eb02856f4a7_Out_0_Vector3, _Property_8b9491b137964a47b72d0c659f60a15c_Out_0_Float, _DirectionMask_e7bee712463546a09117207b42c4d099, _DirectionMask_e7bee712463546a09117207b42c4d099_Out_1_Vector3, _DirectionMask_e7bee712463546a09117207b42c4d099_Mask_2_Vector3, _DirectionMask_e7bee712463546a09117207b42c4d099_ReferenceDirection_3_Vector3);
        float4 _Swizzle_238403211f7e48bdbb15ed1d20a95689_Out_1_Vector4 = _DirectionMask_e7bee712463546a09117207b42c4d099_Mask_2_Vector3.yxzz;
        float3 _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Swizzle_238403211f7e48bdbb15ed1d20a95689_Out_1_Vector4.xyz), _DirectionMask_e7bee712463546a09117207b42c4d099_ReferenceDirection_3_Vector3, _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3);
        float _Split_34e340c38b4d45f087e78448338f6d88_R_1_Float = _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3[0];
        float _Split_34e340c38b4d45f087e78448338f6d88_G_2_Float = _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3[1];
        float _Split_34e340c38b4d45f087e78448338f6d88_B_3_Float = _Multiply_57757d6381924778a1e303799ec8c457_Out_2_Vector3[2];
        float _Split_34e340c38b4d45f087e78448338f6d88_A_4_Float = 0;
        Bindings_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float _MaskModulator_4298e77c3b6c46758ca7300e7e78663a;
        half _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_MaskIntegral_1_Float;
        half _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_Axis_2_Float;
        half _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_NegAxis_3_Float;
        SG_MaskModulator_d89d0a529c8bf9741860f8badb0df8f1_float(_Split_34e340c38b4d45f087e78448338f6d88_G_2_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_MaskIntegral_1_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_Axis_2_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_NegAxis_3_Float);
        Bindings_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float _TriAdditiveFloat_f0321225145e4a639f238d1760d97c03;
        half _TriAdditiveFloat_f0321225145e4a639f238d1760d97c03_Out_1_Float;
        SG_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float(_MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_Axis_2_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_Axis_2_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_Axis_2_Float, 0, _TriAdditiveFloat_f0321225145e4a639f238d1760d97c03, _TriAdditiveFloat_f0321225145e4a639f238d1760d97c03_Out_1_Float);
        float _Property_7b14e93a22e54d68b88fb6bf74202885_Out_0_Float = _Power;
        float _Power_6f359598b5674a5aad9b2689d24b4f33_Out_2_Float;
        Unity_Power_float(_TriAdditiveFloat_f0321225145e4a639f238d1760d97c03_Out_1_Float, _Property_7b14e93a22e54d68b88fb6bf74202885_Out_0_Float, _Power_6f359598b5674a5aad9b2689d24b4f33_Out_2_Float);
        Bindings_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float _TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77;
        half _TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77_Out_1_Float;
        SG_TriAdditiveFloat_0c5d5b6b8b8b4694da654358503cf885_float(_MaskModulator_06d10c70c9604a9e9788d46a3b20e2c9_NegAxis_3_Float, _MaskModulator_c28c129c5ffc4919a7bd892acd67fb40_NegAxis_3_Float, _MaskModulator_4298e77c3b6c46758ca7300e7e78663a_NegAxis_3_Float, 0, _TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77, _TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77_Out_1_Float);
        float _Power_c263b74a39ca4a9ba1c608ce78fbbf3c_Out_2_Float;
        Unity_Power_float(_TriAdditiveFloat_32733cd338a240c9a55c95d6c31b6a77_Out_1_Float, _Property_7b14e93a22e54d68b88fb6bf74202885_Out_0_Float, _Power_c263b74a39ca4a9ba1c608ce78fbbf3c_Out_2_Float);
        Axis_1 = _Power_6f359598b5674a5aad9b2689d24b4f33_Out_2_Float;
        Neg_Axis_2 = _Power_c263b74a39ca4a9ba1c608ce78fbbf3c_Out_2_Float;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Lerp_float(float A, float B, float T, out float Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_SampleGradientV1_float(Gradient Gradient, float Time, out float4 Out)
        {
            // convert to OkLab if we need perceptual color space.
            float3 color = lerp(Gradient.colors[0].rgb, LinearToOklab(Gradient.colors[0].rgb), Gradient.type == 2);
        
            [unroll]
            for (int c = 1; c < Gradient.colorsLength; c++)
            {
                float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
                float3 color2 = lerp(Gradient.colors[c].rgb, LinearToOklab(Gradient.colors[c].rgb), Gradient.type == 2);
                color = lerp(color, color2, lerp(colorPos, step(0.01, colorPos), Gradient.type % 2)); // grad.type == 1 is fixed, 0 and 2 are blends.
            }
            color = lerp(color, OklabToLinear(color), Gradient.type == 2);
        
        #ifdef UNITY_COLORSPACE_GAMMA
            color = LinearToSRGB(color);
        #endif
        
            float alpha = Gradient.alphas[0].x;
            [unroll]
            for (int a = 1; a < Gradient.alphasLength; a++)
            {
                float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
                alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type % 2));
            }
        
            Out = float4(color, alpha);
        }
        
        struct Bindings_IHHSubGraph_210e4d2b552b95f439d320382950a945_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_IHHSubGraph_210e4d2b552b95f439d320382950a945_float(Bindings_IHHSubGraph_210e4d2b552b95f439d320382950a945_float IN, out float OutVector1_1)
        {
        float3 _MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3);
        float3 _Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3);
        float3 _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Normalize_03556ab12c5b4910878478feda4b1ffb_Out_1_Vector3, float3(-1, 0, -1), _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3);
        float _DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float;
        Unity_DotProduct_float3(_MainLightDirection_2fcb6dbc457f46b192b6f1cd0e1ad04f_Direction_0_Vector3, _Multiply_351083c1e6d0419387d843817af9e184_Out_2_Vector3, _DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float);
        float _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float;
        Unity_Saturate_float(_DotProduct_2166b0a63a2d47418ff50c0bab51e472_Out_2_Float, _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float);
        OutVector1_1 = _Saturate_6cd27d919f6148d5a1b1a3e4bf4d2f12_Out_1_Float;
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        void Unity_Clamp_float3(float3 In, float3 Min, float3 Max, out float3 Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Clamp_float4(float4 In, float4 Min, float4 Max, out float4 Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Power_float4(float4 A, float4 B, out float4 Out)
        {
            Out = pow(A, B);
        }
        
        struct Bindings_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float
        {
        float3 ObjectSpaceNormal;
        float3 ObjectSpaceViewDirection;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float3 TimeParameters;
        };
        
        void SG_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float(UnityTextureCube _Starmap, float4 _Sky_Color, float _Exposure, float4 _Tint, float _Sun_Size, float _Cloud_Mask, float _Light_Pollution, float _Haze_Strength, Gradient _Gradient, float _Star_Roration_Speed, Bindings_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float IN, out float3 OutVector3_1)
        {
        float _Float_e4fd19b52466439fa21635069f45b430_Out_0_Float = float(0.95);
        float _Property_59196564f99a4a8fac46ecaeafaf30be_Out_0_Float = _Sun_Size;
        float _Subtract_ff5a72976c724699a42ada81eed745ae_Out_2_Float;
        Unity_Subtract_float(_Float_e4fd19b52466439fa21635069f45b430_Out_0_Float, _Property_59196564f99a4a8fac46ecaeafaf30be_Out_0_Float, _Subtract_ff5a72976c724699a42ada81eed745ae_Out_2_Float);
        float _Multiply_b61ae6740e7645dcb5a976e3dc736f46_Out_2_Float;
        Unity_Multiply_float_float(_Property_59196564f99a4a8fac46ecaeafaf30be_Out_0_Float, 0.01, _Multiply_b61ae6740e7645dcb5a976e3dc736f46_Out_2_Float);
        float _Power_d50dca165d524c15aa71b6ecc3b32de5_Out_2_Float;
        Unity_Power_float(_Multiply_b61ae6740e7645dcb5a976e3dc736f46_Out_2_Float, float(0.5), _Power_d50dca165d524c15aa71b6ecc3b32de5_Out_2_Float);
        float _Add_fb0d23f2b0dd4f4b8fdf7078e8a2117e_Out_2_Float;
        Unity_Add_float(_Subtract_ff5a72976c724699a42ada81eed745ae_Out_2_Float, _Power_d50dca165d524c15aa71b6ecc3b32de5_Out_2_Float, _Add_fb0d23f2b0dd4f4b8fdf7078e8a2117e_Out_2_Float);
        Bindings_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float _SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940;
        _SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940.WorldSpacePosition = IN.WorldSpacePosition;
        float _SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940_OutVector1_1_Float;
        SG_SunDirectionSubGraph_426d3689e5425d341a04b2d76a29cbe4_float(_SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940, _SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940_OutVector1_1_Float);
        float _Power_7ec402ff3ffe491898f147559ae6f309_Out_2_Float;
        Unity_Power_float(_SunDirectionSubGraph_9d018b4ff9cf4388a897d85d443d3940_OutVector1_1_Float, float(60), _Power_7ec402ff3ffe491898f147559ae6f309_Out_2_Float);
        float _Smoothstep_fac4d918b1bb4f96ad318fd7d025e4e1_Out_3_Float;
        Unity_Smoothstep_float(_Subtract_ff5a72976c724699a42ada81eed745ae_Out_2_Float, _Add_fb0d23f2b0dd4f4b8fdf7078e8a2117e_Out_2_Float, _Power_7ec402ff3ffe491898f147559ae6f309_Out_2_Float, _Smoothstep_fac4d918b1bb4f96ad318fd7d025e4e1_Out_3_Float);
        float4 Color_632156c4356f4f12925a118136eb5c05 = IsGammaSpace() ? LinearToSRGB(float4(16, 16, 16, 0)) : float4(16, 16, 16, 0);
        Bindings_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151;
        _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Direction_1_Vector3;
        float3 _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Color_2_Vector3;
        float _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_ShadowAtten_3_Float;
        SG_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float(half3 (0, 0, 0), false, _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151, _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Direction_1_Vector3, _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Color_2_Vector3, _GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_ShadowAtten_3_Float);
        float3 _Saturation_96a051ca05a64dcba47d2336f6a8e5ff_Out_2_Vector3;
        Unity_Saturation_float(_GetMainLightCustom_eff6848916ab4aa0b9e516c23fee3151_Color_2_Vector3, float(0), _Saturation_96a051ca05a64dcba47d2336f6a8e5ff_Out_2_Vector3);
        float _Float_b84f90b58b5b4558a94da49ddded5d5d_Out_0_Float = float(0.05);
        float3 _Maximum_579bbfe6a6f343cb9b4330031cdbe8de_Out_2_Vector3;
        Unity_Maximum_float3(_Saturation_96a051ca05a64dcba47d2336f6a8e5ff_Out_2_Vector3, (_Float_b84f90b58b5b4558a94da49ddded5d5d_Out_0_Float.xxx), _Maximum_579bbfe6a6f343cb9b4330031cdbe8de_Out_2_Vector3);
        float3 _Ceiling_4e601f08b30d49e6b238932653d00538_Out_1_Vector3;
        Unity_Ceiling_float3(_Saturation_96a051ca05a64dcba47d2336f6a8e5ff_Out_2_Vector3, _Ceiling_4e601f08b30d49e6b238932653d00538_Out_1_Vector3);
        float _Branch_5aa75ae8578946f1bec3ad4fd4a7a435_Out_3_Float;
        Unity_Branch_float(((bool) _Ceiling_4e601f08b30d49e6b238932653d00538_Out_1_Vector3.x), float(1), float(0), _Branch_5aa75ae8578946f1bec3ad4fd4a7a435_Out_3_Float);
        float3 _Multiply_bd06f1f066fe46d581b744594405f39f_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Maximum_579bbfe6a6f343cb9b4330031cdbe8de_Out_2_Vector3, (_Branch_5aa75ae8578946f1bec3ad4fd4a7a435_Out_3_Float.xxx), _Multiply_bd06f1f066fe46d581b744594405f39f_Out_2_Vector3);
        float3 _Multiply_ba90c1d6677a47ecae920085e7178adb_Out_2_Vector3;
        Unity_Multiply_float3_float3((Color_632156c4356f4f12925a118136eb5c05.xyz), _Multiply_bd06f1f066fe46d581b744594405f39f_Out_2_Vector3, _Multiply_ba90c1d6677a47ecae920085e7178adb_Out_2_Vector3);
        float3 _Multiply_00872863b4e641a4895950d8254775ec_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Smoothstep_fac4d918b1bb4f96ad318fd7d025e4e1_Out_3_Float.xxx), _Multiply_ba90c1d6677a47ecae920085e7178adb_Out_2_Vector3, _Multiply_00872863b4e641a4895950d8254775ec_Out_2_Vector3);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_0b72deb735fc442e8fb0228371f79878;
        _BottomMargin_0b72deb735fc442e8fb0228371f79878.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_0b72deb735fc442e8fb0228371f79878_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_0b72deb735fc442e8fb0228371f79878, _BottomMargin_0b72deb735fc442e8fb0228371f79878_OutVector1_1_Float);
        float3 _Multiply_5944a8d7f2834ef6bf339c32906f13dd_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_00872863b4e641a4895950d8254775ec_Out_2_Vector3, (_BottomMargin_0b72deb735fc442e8fb0228371f79878_OutVector1_1_Float.xxx), _Multiply_5944a8d7f2834ef6bf339c32906f13dd_Out_2_Vector3);
        Gradient _Property_01e6112c6b2441c5b6e6fdac20b773b1_Out_0_Gradient = _Gradient;
        Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float _G_0f0d5716ee584deab6b4e7ee1dce336c;
        _G_0f0d5716ee584deab6b4e7ee1dce336c.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half _G_0f0d5716ee584deab6b4e7ee1dce336c_Axis_1_Float;
        half _G_0f0d5716ee584deab6b4e7ee1dce336c_NegAxis_2_Float;
        SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(half(1), half(0), _G_0f0d5716ee584deab6b4e7ee1dce336c, _G_0f0d5716ee584deab6b4e7ee1dce336c_Axis_1_Float, _G_0f0d5716ee584deab6b4e7ee1dce336c_NegAxis_2_Float);
        float _Subtract_6d93dab28c4d4911be067aa2c15c0c9b_Out_2_Float;
        Unity_Subtract_float(_G_0f0d5716ee584deab6b4e7ee1dce336c_Axis_1_Float, _G_0f0d5716ee584deab6b4e7ee1dce336c_NegAxis_2_Float, _Subtract_6d93dab28c4d4911be067aa2c15c0c9b_Out_2_Float);
        float _Add_018defcefa6942e29deb10b9cf378578_Out_2_Float;
        Unity_Add_float(_Subtract_6d93dab28c4d4911be067aa2c15c0c9b_Out_2_Float, float(1), _Add_018defcefa6942e29deb10b9cf378578_Out_2_Float);
        float _Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float;
        Unity_Multiply_float_float(_Add_018defcefa6942e29deb10b9cf378578_Out_2_Float, 0.5, _Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float);
        float3 _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3);
        float _Split_bbe6128b941e41979d83d67f57627849_R_1_Float = _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3[0];
        float _Split_bbe6128b941e41979d83d67f57627849_G_2_Float = _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3[1];
        float _Split_bbe6128b941e41979d83d67f57627849_B_3_Float = _Normalize_7e884cedcd5043ad8f42d54477994b22_Out_1_Vector3[2];
        float _Split_bbe6128b941e41979d83d67f57627849_A_4_Float = 0;
        float _Absolute_51a90082a9054d91bfca64f7e5297c49_Out_1_Float;
        Unity_Absolute_float(_Split_bbe6128b941e41979d83d67f57627849_G_2_Float, _Absolute_51a90082a9054d91bfca64f7e5297c49_Out_1_Float);
        float _Saturate_ee601b895f834d6ca30749b19481113e_Out_1_Float;
        Unity_Saturate_float(_Absolute_51a90082a9054d91bfca64f7e5297c49_Out_1_Float, _Saturate_ee601b895f834d6ca30749b19481113e_Out_1_Float);
        float _OneMinus_211681ab551d471896d9f1d5252a8bba_Out_1_Float;
        Unity_OneMinus_float(_Saturate_ee601b895f834d6ca30749b19481113e_Out_1_Float, _OneMinus_211681ab551d471896d9f1d5252a8bba_Out_1_Float);
        float _Property_da2c1d123fe54d8a9f31b28775812194_Out_0_Float = _Haze_Strength;
        float _Lerp_1b233567c69f4d6b8f094fe0ba5adeb2_Out_3_Float;
        Unity_Lerp_float(float(12), float(3.5), _Property_da2c1d123fe54d8a9f31b28775812194_Out_0_Float, _Lerp_1b233567c69f4d6b8f094fe0ba5adeb2_Out_3_Float);
        float _Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float;
        Unity_Power_float(_OneMinus_211681ab551d471896d9f1d5252a8bba_Out_1_Float, _Lerp_1b233567c69f4d6b8f094fe0ba5adeb2_Out_3_Float, _Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float);
        float _OneMinus_c541cbd17da24b12b4494c2ce12f97a5_Out_1_Float;
        Unity_OneMinus_float(_Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float, _OneMinus_c541cbd17da24b12b4494c2ce12f97a5_Out_1_Float);
        float _Multiply_1c5f1de5dd5f431eb3c758d3a4b59287_Out_2_Float;
        Unity_Multiply_float_float(0.1, _OneMinus_c541cbd17da24b12b4494c2ce12f97a5_Out_1_Float, _Multiply_1c5f1de5dd5f431eb3c758d3a4b59287_Out_2_Float);
        float _Add_73f28746822a42629e0054eb2bc823ee_Out_2_Float;
        Unity_Add_float(_Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float, _Multiply_1c5f1de5dd5f431eb3c758d3a4b59287_Out_2_Float, _Add_73f28746822a42629e0054eb2bc823ee_Out_2_Float);
        float _Clamp_41f7371ccc544da698ec435f3222fa6e_Out_3_Float;
        Unity_Clamp_float(_Add_73f28746822a42629e0054eb2bc823ee_Out_2_Float, float(0), float(1), _Clamp_41f7371ccc544da698ec435f3222fa6e_Out_3_Float);
        float4 _SampleGradient_f0175d87f8264a2b9039318f0bd895ff_Out_2_Vector4;
        Unity_SampleGradientV1_float(_Property_01e6112c6b2441c5b6e6fdac20b773b1_Out_0_Gradient, _Clamp_41f7371ccc544da698ec435f3222fa6e_Out_3_Float, _SampleGradient_f0175d87f8264a2b9039318f0bd895ff_Out_2_Vector4);
        float3 _Multiply_3508a2d2ba5045379eb5df9463216a71_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_5944a8d7f2834ef6bf339c32906f13dd_Out_2_Vector3, (_SampleGradient_f0175d87f8264a2b9039318f0bd895ff_Out_2_Vector4.xyz), _Multiply_3508a2d2ba5045379eb5df9463216a71_Out_2_Vector3);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_df937e50d90649c3ae85a6152fbecd6d;
        _BottomMargin_df937e50d90649c3ae85a6152fbecd6d.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_df937e50d90649c3ae85a6152fbecd6d_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_df937e50d90649c3ae85a6152fbecd6d, _BottomMargin_df937e50d90649c3ae85a6152fbecd6d_OutVector1_1_Float);
        Bindings_IHHSubGraph_210e4d2b552b95f439d320382950a945_float _IHHSubGraph_916ccb4a130d41bea16700c09fec0520;
        _IHHSubGraph_916ccb4a130d41bea16700c09fec0520.WorldSpacePosition = IN.WorldSpacePosition;
        float _IHHSubGraph_916ccb4a130d41bea16700c09fec0520_OutVector1_1_Float;
        SG_IHHSubGraph_210e4d2b552b95f439d320382950a945_float(_IHHSubGraph_916ccb4a130d41bea16700c09fec0520, _IHHSubGraph_916ccb4a130d41bea16700c09fec0520_OutVector1_1_Float);
        float _Power_3cfbe5a64f9b43c3a940ef678f7fe195_Out_2_Float;
        Unity_Power_float(_IHHSubGraph_916ccb4a130d41bea16700c09fec0520_OutVector1_1_Float, float(1.5), _Power_3cfbe5a64f9b43c3a940ef678f7fe195_Out_2_Float);
        Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float _G_6101fcecd7114972b1efaa6c13168cae;
        _G_6101fcecd7114972b1efaa6c13168cae.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half _G_6101fcecd7114972b1efaa6c13168cae_Axis_1_Float;
        half _G_6101fcecd7114972b1efaa6c13168cae_NegAxis_2_Float;
        SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(half(1), half(0), _G_6101fcecd7114972b1efaa6c13168cae, _G_6101fcecd7114972b1efaa6c13168cae_Axis_1_Float, _G_6101fcecd7114972b1efaa6c13168cae_NegAxis_2_Float);
        float _Add_c489d26d25894e5780a8cec97f2bdec5_Out_2_Float;
        Unity_Add_float(_Power_3cfbe5a64f9b43c3a940ef678f7fe195_Out_2_Float, _G_6101fcecd7114972b1efaa6c13168cae_Axis_1_Float, _Add_c489d26d25894e5780a8cec97f2bdec5_Out_2_Float);
        float _Multiply_1cf4a85661df41d5a8cb6f01b5bcf650_Out_2_Float;
        Unity_Multiply_float_float(_Add_c489d26d25894e5780a8cec97f2bdec5_Out_2_Float, 0.5, _Multiply_1cf4a85661df41d5a8cb6f01b5bcf650_Out_2_Float);
        float _Multiply_d9eb194aaaec4c5e90be162870c3cc89_Out_2_Float;
        Unity_Multiply_float_float(_BottomMargin_df937e50d90649c3ae85a6152fbecd6d_OutVector1_1_Float, _Multiply_1cf4a85661df41d5a8cb6f01b5bcf650_Out_2_Float, _Multiply_d9eb194aaaec4c5e90be162870c3cc89_Out_2_Float);
        float3 _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3;
        Unity_Normalize_float3(IN.WorldSpacePosition, _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3);
        float _Split_d5af13543d894489a14f9332ac28c9d1_R_1_Float = _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3[0];
        float _Split_d5af13543d894489a14f9332ac28c9d1_G_2_Float = _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3[1];
        float _Split_d5af13543d894489a14f9332ac28c9d1_B_3_Float = _Normalize_009d97cf3c1d49c69e77280cf5beb123_Out_1_Vector3[2];
        float _Split_d5af13543d894489a14f9332ac28c9d1_A_4_Float = 0;
        float _Saturate_7a71970aab60438ca7b7b20d52f1d135_Out_1_Float;
        Unity_Saturate_float(_Split_d5af13543d894489a14f9332ac28c9d1_G_2_Float, _Saturate_7a71970aab60438ca7b7b20d52f1d135_Out_1_Float);
        float _OneMinus_92e84c85e4d9493583d6ec523f4aac6e_Out_1_Float;
        Unity_OneMinus_float(_Saturate_7a71970aab60438ca7b7b20d52f1d135_Out_1_Float, _OneMinus_92e84c85e4d9493583d6ec523f4aac6e_Out_1_Float);
        float _Add_a3994157d05445a090095b25a88446c6_Out_2_Float;
        Unity_Add_float(_OneMinus_92e84c85e4d9493583d6ec523f4aac6e_Out_1_Float, float(0.5), _Add_a3994157d05445a090095b25a88446c6_Out_2_Float);
        float _Multiply_c68f3e26bea14361923f242fdec0048e_Out_2_Float;
        Unity_Multiply_float_float(_Split_d5af13543d894489a14f9332ac28c9d1_G_2_Float, -1, _Multiply_c68f3e26bea14361923f242fdec0048e_Out_2_Float);
        float _Saturate_63a0f6660f2748f6a73d6af628168a5c_Out_1_Float;
        Unity_Saturate_float(_Multiply_c68f3e26bea14361923f242fdec0048e_Out_2_Float, _Saturate_63a0f6660f2748f6a73d6af628168a5c_Out_1_Float);
        float _OneMinus_003371d3b83c48f1ad1d2513343275e9_Out_1_Float;
        Unity_OneMinus_float(_Saturate_63a0f6660f2748f6a73d6af628168a5c_Out_1_Float, _OneMinus_003371d3b83c48f1ad1d2513343275e9_Out_1_Float);
        float _Power_f40ba80da1f347c0bd0f9164db5b000d_Out_2_Float;
        Unity_Power_float(_OneMinus_003371d3b83c48f1ad1d2513343275e9_Out_1_Float, float(10), _Power_f40ba80da1f347c0bd0f9164db5b000d_Out_2_Float);
        float _Multiply_781e862100d24fc0a6c5dccc0b8f6eb6_Out_2_Float;
        Unity_Multiply_float_float(_OneMinus_003371d3b83c48f1ad1d2513343275e9_Out_1_Float, 0, _Multiply_781e862100d24fc0a6c5dccc0b8f6eb6_Out_2_Float);
        float _Add_1edd0ddefdb24161873aa30af48fd4c9_Out_2_Float;
        Unity_Add_float(_Power_f40ba80da1f347c0bd0f9164db5b000d_Out_2_Float, _Multiply_781e862100d24fc0a6c5dccc0b8f6eb6_Out_2_Float, _Add_1edd0ddefdb24161873aa30af48fd4c9_Out_2_Float);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_f2350a746ddc4611aaf144efd7ed6afc;
        _BottomMargin_f2350a746ddc4611aaf144efd7ed6afc.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_f2350a746ddc4611aaf144efd7ed6afc_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_f2350a746ddc4611aaf144efd7ed6afc, _BottomMargin_f2350a746ddc4611aaf144efd7ed6afc_OutVector1_1_Float);
        float _OneMinus_e484be172bd249d88f24864364899ac1_Out_1_Float;
        Unity_OneMinus_float(_BottomMargin_f2350a746ddc4611aaf144efd7ed6afc_OutVector1_1_Float, _OneMinus_e484be172bd249d88f24864364899ac1_Out_1_Float);
        float _Multiply_285173e3363b443eb6af8b6ac02fd1ee_Out_2_Float;
        Unity_Multiply_float_float(_OneMinus_e484be172bd249d88f24864364899ac1_Out_1_Float, 0.4, _Multiply_285173e3363b443eb6af8b6ac02fd1ee_Out_2_Float);
        float _Add_435ab1e14a7e426b818a4757dd947795_Out_2_Float;
        Unity_Add_float(_Add_1edd0ddefdb24161873aa30af48fd4c9_Out_2_Float, _Multiply_285173e3363b443eb6af8b6ac02fd1ee_Out_2_Float, _Add_435ab1e14a7e426b818a4757dd947795_Out_2_Float);
        float _Power_cae5ba0350c04de4b7411ba4e1dd8377_Out_2_Float;
        Unity_Power_float(_Add_435ab1e14a7e426b818a4757dd947795_Out_2_Float, float(2.56), _Power_cae5ba0350c04de4b7411ba4e1dd8377_Out_2_Float);
        float _Minimum_0418f0df67084b5fa98211b3ddfbfac6_Out_2_Float;
        Unity_Minimum_float(_Add_a3994157d05445a090095b25a88446c6_Out_2_Float, _Power_cae5ba0350c04de4b7411ba4e1dd8377_Out_2_Float, _Minimum_0418f0df67084b5fa98211b3ddfbfac6_Out_2_Float);
        float _Add_68666b5746e94f8b904084c8ee551960_Out_2_Float;
        Unity_Add_float(_Multiply_d9eb194aaaec4c5e90be162870c3cc89_Out_2_Float, _Minimum_0418f0df67084b5fa98211b3ddfbfac6_Out_2_Float, _Add_68666b5746e94f8b904084c8ee551960_Out_2_Float);
        float4 _Property_b1198c3ca5d645948d704651058e1065_Out_0_Vector4 = _Sky_Color;
        float4 _Multiply_62d6371e66d74e2a80dd74ca1f4b0e57_Out_2_Vector4;
        Unity_Multiply_float4_float4((_Add_68666b5746e94f8b904084c8ee551960_Out_2_Float.xxxx), _Property_b1198c3ca5d645948d704651058e1065_Out_0_Vector4, _Multiply_62d6371e66d74e2a80dd74ca1f4b0e57_Out_2_Vector4);
        float _Property_b4b5e54fbcc044c1840d135b415c09b6_Out_0_Float = _Light_Pollution;
        float _Multiply_970c06ab37864e5a8910ec8cd3f4bd84_Out_2_Float;
        Unity_Multiply_float_float(_Property_b4b5e54fbcc044c1840d135b415c09b6_Out_0_Float, 0.1, _Multiply_970c06ab37864e5a8910ec8cd3f4bd84_Out_2_Float);
        Bindings_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089;
        _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Direction_1_Vector3;
        float3 _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Color_2_Vector3;
        float _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_ShadowAtten_3_Float;
        SG_GetMainLightCustom_0f51a8bac361de4439adb5bbd7c19a1b_float(half3 (0, 0, 0), false, _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089, _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Direction_1_Vector3, _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Color_2_Vector3, _GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_ShadowAtten_3_Float);
        float3 _Saturation_fb51bbc95e0c451bb2ca3bdcfb900b24_Out_2_Vector3;
        Unity_Saturation_float(_GetMainLightCustom_0ec7f90ca13642aebf5b6a23f4dc0089_Color_2_Vector3, float(0), _Saturation_fb51bbc95e0c451bb2ca3bdcfb900b24_Out_2_Vector3);
        float _Float_6edff186ef7940b9bc77f6f47b26aad4_Out_0_Float = float(0);
        float3 _Clamp_8f3f8cad7d3b49e3a06a733a147c5d75_Out_3_Vector3;
        Unity_Clamp_float3(_Saturation_fb51bbc95e0c451bb2ca3bdcfb900b24_Out_2_Vector3, (_Float_6edff186ef7940b9bc77f6f47b26aad4_Out_0_Float.xxx), float3(1, 1, 1), _Clamp_8f3f8cad7d3b49e3a06a733a147c5d75_Out_3_Vector3);
        float _Power_d03a2de7a40b4db2b738c715112a18e8_Out_2_Float;
        Unity_Power_float(_Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float, float(0.8), _Power_d03a2de7a40b4db2b738c715112a18e8_Out_2_Float);
        float _Remap_ce3c7bee85a1457baebf620a9b6b587b_Out_3_Float;
        Unity_Remap_float(_Power_d03a2de7a40b4db2b738c715112a18e8_Out_2_Float, float2 (0.45, 0.6), float2 (0, 1), _Remap_ce3c7bee85a1457baebf620a9b6b587b_Out_3_Float);
        float _Clamp_827779339a274fdc88d3e698ae011970_Out_3_Float;
        Unity_Clamp_float(_Remap_ce3c7bee85a1457baebf620a9b6b587b_Out_3_Float, float(0), float(1), _Clamp_827779339a274fdc88d3e698ae011970_Out_3_Float);
        float3 _Multiply_75735188908a4a57a9be75eff01d3ea3_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Clamp_8f3f8cad7d3b49e3a06a733a147c5d75_Out_3_Vector3, (_Clamp_827779339a274fdc88d3e698ae011970_Out_3_Float.xxx), _Multiply_75735188908a4a57a9be75eff01d3ea3_Out_2_Vector3);
        float3 _Add_5acf440c7f8341eaa017d29d984f3565_Out_2_Vector3;
        Unity_Add_float3((_Multiply_970c06ab37864e5a8910ec8cd3f4bd84_Out_2_Float.xxx), _Multiply_75735188908a4a57a9be75eff01d3ea3_Out_2_Vector3, _Add_5acf440c7f8341eaa017d29d984f3565_Out_2_Vector3);
        float3 _Multiply_e20ad02e76cb494d9711959e7ab14d9b_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Multiply_62d6371e66d74e2a80dd74ca1f4b0e57_Out_2_Vector4.xyz), _Add_5acf440c7f8341eaa017d29d984f3565_Out_2_Vector3, _Multiply_e20ad02e76cb494d9711959e7ab14d9b_Out_2_Vector3);
        float4 _SampleGradient_e6f402b67de946658f3d49f8ef2c26d2_Out_2_Vector4;
        Unity_SampleGradientV1_float(NewGradient(0, 6, 2, float4(1, 1, 1, 0),float4(4, 0.4539363, 0, 0.4699931),float4(2.996078, 0.493884, 0, 0.5000076),float4(2.996078, 1.553205, 0, 0.6),float4(2.118547, 2.118547, 2.118547, 0.7000076),float4(1, 1, 1, 0.8),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _Multiply_b73705f239b84557a7f94608f74a7338_Out_2_Float, _SampleGradient_e6f402b67de946658f3d49f8ef2c26d2_Out_2_Vector4);
        float4 _Clamp_edb58ed54f9d4d52803d752ada950ce9_Out_3_Vector4;
        Unity_Clamp_float4(_SampleGradient_e6f402b67de946658f3d49f8ef2c26d2_Out_2_Vector4, float4(0, 0, 0, 0), float4(1, 1, 1, 1), _Clamp_edb58ed54f9d4d52803d752ada950ce9_Out_3_Vector4);
        float3 _Saturation_b2a0668a693c425f8d4c3a925a5eedd1_Out_2_Vector3;
        Unity_Saturation_float((_Clamp_edb58ed54f9d4d52803d752ada950ce9_Out_3_Vector4.xyz), float(0.3), _Saturation_b2a0668a693c425f8d4c3a925a5eedd1_Out_2_Vector3);
        float3 _Multiply_e530e010f9744147b0a688abde27ec68_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_e20ad02e76cb494d9711959e7ab14d9b_Out_2_Vector3, _Saturation_b2a0668a693c425f8d4c3a925a5eedd1_Out_2_Vector3, _Multiply_e530e010f9744147b0a688abde27ec68_Out_2_Vector3);
        float _Float_f5bc9d95c9854659bbc5161a351207e5_Out_0_Float = float(1);
        float4 _Multiply_3e0c4b5d8935412abb84031816ad7c7d_Out_2_Vector4;
        Unity_Multiply_float4_float4(_SampleGradient_f0175d87f8264a2b9039318f0bd895ff_Out_2_Vector4, (_Float_f5bc9d95c9854659bbc5161a351207e5_Out_0_Float.xxxx), _Multiply_3e0c4b5d8935412abb84031816ad7c7d_Out_2_Vector4);
        float3 _Multiply_9729309c7109442fada0a8ff08b56242_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Multiply_75735188908a4a57a9be75eff01d3ea3_Out_2_Vector3, (_Multiply_3e0c4b5d8935412abb84031816ad7c7d_Out_2_Vector4.xyz), _Multiply_9729309c7109442fada0a8ff08b56242_Out_2_Vector3);
        float _Float_057d99bc350e40109468e94e4ba84375_Out_0_Float = float(0.2);
        Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float _G_07eb6ef6380a42c78a0936f68a0ab3f1;
        _G_07eb6ef6380a42c78a0936f68a0ab3f1.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half _G_07eb6ef6380a42c78a0936f68a0ab3f1_Axis_1_Float;
        half _G_07eb6ef6380a42c78a0936f68a0ab3f1_NegAxis_2_Float;
        SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(half(1), half(0), _G_07eb6ef6380a42c78a0936f68a0ab3f1, _G_07eb6ef6380a42c78a0936f68a0ab3f1_Axis_1_Float, _G_07eb6ef6380a42c78a0936f68a0ab3f1_NegAxis_2_Float);
        float _Add_289995e9aebf4301b18b6f92e147b346_Out_2_Float;
        Unity_Add_float(_G_07eb6ef6380a42c78a0936f68a0ab3f1_NegAxis_2_Float, float(-0.04), _Add_289995e9aebf4301b18b6f92e147b346_Out_2_Float);
        float _Saturate_7ee7373d114f400faeb08e2cf0d4307c_Out_1_Float;
        Unity_Saturate_float(_Add_289995e9aebf4301b18b6f92e147b346_Out_2_Float, _Saturate_7ee7373d114f400faeb08e2cf0d4307c_Out_1_Float);
        float _Power_a9547f5d3a464e4790e82476762876d4_Out_2_Float;
        Unity_Power_float(_Saturate_7ee7373d114f400faeb08e2cf0d4307c_Out_1_Float, float(0.4), _Power_a9547f5d3a464e4790e82476762876d4_Out_2_Float);
        float _Multiply_df4d258607d44486a11449e64d6852be_Out_2_Float;
        Unity_Multiply_float_float(_Power_a9547f5d3a464e4790e82476762876d4_Out_2_Float, 2, _Multiply_df4d258607d44486a11449e64d6852be_Out_2_Float);
        float _Add_a208a1d796f44559b0a318d613708443_Out_2_Float;
        Unity_Add_float(_Multiply_df4d258607d44486a11449e64d6852be_Out_2_Float, float(-0.3), _Add_a208a1d796f44559b0a318d613708443_Out_2_Float);
        float _Clamp_171cd3e4edf64285a8dac6dbeb8bb44e_Out_3_Float;
        Unity_Clamp_float(_Add_a208a1d796f44559b0a318d613708443_Out_2_Float, float(-0.3), float(0.7), _Clamp_171cd3e4edf64285a8dac6dbeb8bb44e_Out_3_Float);
        float _Float_5f196b71832c4e6d800eb1b79fc566ca_Out_0_Float = _Clamp_171cd3e4edf64285a8dac6dbeb8bb44e_Out_3_Float;
        float _Add_c423554b0b034276a07a9b3c17fdf4f3_Out_2_Float;
        Unity_Add_float(_Float_5f196b71832c4e6d800eb1b79fc566ca_Out_0_Float, float(0.7), _Add_c423554b0b034276a07a9b3c17fdf4f3_Out_2_Float);
        float _OneMinus_5520d3ebf29441d38417bdded937f256_Out_1_Float;
        Unity_OneMinus_float(_Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float, _OneMinus_5520d3ebf29441d38417bdded937f256_Out_1_Float);
        float _Smoothstep_c6865d71edaa45cd9d71e464ef289261_Out_3_Float;
        Unity_Smoothstep_float(_Float_5f196b71832c4e6d800eb1b79fc566ca_Out_0_Float, _Add_c423554b0b034276a07a9b3c17fdf4f3_Out_2_Float, _OneMinus_5520d3ebf29441d38417bdded937f256_Out_1_Float, _Smoothstep_c6865d71edaa45cd9d71e464ef289261_Out_3_Float);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_0a381e7cb297469ab40600952faa97cb;
        _BottomMargin_0a381e7cb297469ab40600952faa97cb.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_0a381e7cb297469ab40600952faa97cb_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_0a381e7cb297469ab40600952faa97cb, _BottomMargin_0a381e7cb297469ab40600952faa97cb_OutVector1_1_Float);
        float _Multiply_04ec0d3c3090428598951334d867a9cf_Out_2_Float;
        Unity_Multiply_float_float(_Smoothstep_c6865d71edaa45cd9d71e464ef289261_Out_3_Float, _BottomMargin_0a381e7cb297469ab40600952faa97cb_OutVector1_1_Float, _Multiply_04ec0d3c3090428598951334d867a9cf_Out_2_Float);
        float _Float_0f1f82d6f8a648dbaa02daeb8bb806d5_Out_0_Float = _Multiply_04ec0d3c3090428598951334d867a9cf_Out_2_Float;
        float _Multiply_28d44dd891754ed9bc34c2f8659cb649_Out_2_Float;
        Unity_Multiply_float_float(_Float_057d99bc350e40109468e94e4ba84375_Out_0_Float, _Float_0f1f82d6f8a648dbaa02daeb8bb806d5_Out_0_Float, _Multiply_28d44dd891754ed9bc34c2f8659cb649_Out_2_Float);
        Bindings_G_4e8d6a2e4714b574ebb400edb19a24ef_float _G_df7eb56be6394e2ba4b1c697d988796a;
        _G_df7eb56be6394e2ba4b1c697d988796a.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half _G_df7eb56be6394e2ba4b1c697d988796a_Axis_1_Float;
        half _G_df7eb56be6394e2ba4b1c697d988796a_NegAxis_2_Float;
        SG_G_4e8d6a2e4714b574ebb400edb19a24ef_float(half(1), half(0), _G_df7eb56be6394e2ba4b1c697d988796a, _G_df7eb56be6394e2ba4b1c697d988796a_Axis_1_Float, _G_df7eb56be6394e2ba4b1c697d988796a_NegAxis_2_Float);
        float _OneMinus_40f689075b014f20a003f776e474ed41_Out_1_Float;
        Unity_OneMinus_float(_G_df7eb56be6394e2ba4b1c697d988796a_Axis_1_Float, _OneMinus_40f689075b014f20a003f776e474ed41_Out_1_Float);
        float _Power_ce04259387f04dcc8e6ce87bcbfcd9ba_Out_2_Float;
        Unity_Power_float(_OneMinus_40f689075b014f20a003f776e474ed41_Out_1_Float, float(0.1), _Power_ce04259387f04dcc8e6ce87bcbfcd9ba_Out_2_Float);
        Bindings_IHHSubGraph_210e4d2b552b95f439d320382950a945_float _IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90;
        _IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90.WorldSpacePosition = IN.WorldSpacePosition;
        float _IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90_OutVector1_1_Float;
        SG_IHHSubGraph_210e4d2b552b95f439d320382950a945_float(_IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90, _IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90_OutVector1_1_Float);
        float _Power_1cfcdb48c95d4629949b15d90df2533c_Out_2_Float;
        Unity_Power_float(_IHHSubGraph_5b6ecd6c71a8463d91460e5f44a55c90_OutVector1_1_Float, float(1.5), _Power_1cfcdb48c95d4629949b15d90df2533c_Out_2_Float);
        float _Power_ba6165353f64456a8c9387bd3ff533d0_Out_2_Float;
        Unity_Power_float(_G_df7eb56be6394e2ba4b1c697d988796a_Axis_1_Float, float(1.5), _Power_ba6165353f64456a8c9387bd3ff533d0_Out_2_Float);
        float _Multiply_2126d490984d4be5bdfe31394daaefeb_Out_2_Float;
        Unity_Multiply_float_float(_Power_ba6165353f64456a8c9387bd3ff533d0_Out_2_Float, 2, _Multiply_2126d490984d4be5bdfe31394daaefeb_Out_2_Float);
        float _Maximum_f0cea5402e81447b9b74e49db3d5a701_Out_2_Float;
        Unity_Maximum_float(_Power_1cfcdb48c95d4629949b15d90df2533c_Out_2_Float, _Multiply_2126d490984d4be5bdfe31394daaefeb_Out_2_Float, _Maximum_f0cea5402e81447b9b74e49db3d5a701_Out_2_Float);
        float _Multiply_a753e72afb5942da802fbaa508147fa8_Out_2_Float;
        Unity_Multiply_float_float(_Power_ce04259387f04dcc8e6ce87bcbfcd9ba_Out_2_Float, _Maximum_f0cea5402e81447b9b74e49db3d5a701_Out_2_Float, _Multiply_a753e72afb5942da802fbaa508147fa8_Out_2_Float);
        float _Lerp_6f838ef66c1648038b30440bc8ef7617_Out_3_Float;
        Unity_Lerp_float(_Multiply_28d44dd891754ed9bc34c2f8659cb649_Out_2_Float, _Float_0f1f82d6f8a648dbaa02daeb8bb806d5_Out_0_Float, _Multiply_a753e72afb5942da802fbaa508147fa8_Out_2_Float, _Lerp_6f838ef66c1648038b30440bc8ef7617_Out_3_Float);
        float _Multiply_f0f7f5ae35874162b2cbf9857300afc2_Out_2_Float;
        Unity_Multiply_float_float(_Power_46aaecf322874f7c9324fc74a0b8cf0e_Out_2_Float, _Lerp_6f838ef66c1648038b30440bc8ef7617_Out_3_Float, _Multiply_f0f7f5ae35874162b2cbf9857300afc2_Out_2_Float);
        float _Multiply_a844ec42b5c94c0e8d8e0dbe805be790_Out_2_Float;
        Unity_Multiply_float_float(_Clamp_827779339a274fdc88d3e698ae011970_Out_3_Float, _Multiply_f0f7f5ae35874162b2cbf9857300afc2_Out_2_Float, _Multiply_a844ec42b5c94c0e8d8e0dbe805be790_Out_2_Float);
        float3 _Lerp_c4d456471c744b49b9faab47a4fc5bd1_Out_3_Vector3;
        Unity_Lerp_float3(_Multiply_e530e010f9744147b0a688abde27ec68_Out_2_Vector3, _Multiply_9729309c7109442fada0a8ff08b56242_Out_2_Vector3, (_Multiply_a844ec42b5c94c0e8d8e0dbe805be790_Out_2_Float.xxx), _Lerp_c4d456471c744b49b9faab47a4fc5bd1_Out_3_Vector3);
        float3 _Add_d8a7191925d1427185d1064714fd74d2_Out_2_Vector3;
        Unity_Add_float3(_Multiply_3508a2d2ba5045379eb5df9463216a71_Out_2_Vector3, _Lerp_c4d456471c744b49b9faab47a4fc5bd1_Out_3_Vector3, _Add_d8a7191925d1427185d1064714fd74d2_Out_2_Vector3);
        float _Property_4febc69456f24ff594aaff18cc78e70a_Out_0_Float = _Exposure;
        float3 _Multiply_4abce12d04e6472ebd0379bfaec50e20_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Add_d8a7191925d1427185d1064714fd74d2_Out_2_Vector3, (_Property_4febc69456f24ff594aaff18cc78e70a_Out_0_Float.xxx), _Multiply_4abce12d04e6472ebd0379bfaec50e20_Out_2_Vector3);
        UnityTextureCube _Property_7a236dc7b986418295c055925dd74f56_Out_0_Cubemap = _Starmap;
        float3 _Normalize_c60bad13e530400e9acdf25d9a102cee_Out_1_Vector3;
        Unity_Normalize_float3(IN.ObjectSpaceNormal, _Normalize_c60bad13e530400e9acdf25d9a102cee_Out_1_Vector3);
        float _Property_58bbd1cfb63e4b3e93aa7d9537e93919_Out_0_Float = _Star_Roration_Speed;
        float _Multiply_caf37e3f96fd454592ee1571efd643e2_Out_2_Float;
        Unity_Multiply_float_float(IN.TimeParameters.x, _Property_58bbd1cfb63e4b3e93aa7d9537e93919_Out_0_Float, _Multiply_caf37e3f96fd454592ee1571efd643e2_Out_2_Float);
        float3 _RotateAboutAxis_18feb3536cc341de8dccef57e7646c24_Out_3_Vector3;
        Unity_Rotate_About_Axis_Degrees_float(_Normalize_c60bad13e530400e9acdf25d9a102cee_Out_1_Vector3, float3 (1, 0, 0), _Multiply_caf37e3f96fd454592ee1571efd643e2_Out_2_Float, _RotateAboutAxis_18feb3536cc341de8dccef57e7646c24_Out_3_Vector3);
        float4 _SampleReflectedCubemap_1f499bf7f9484e52a24768d66256410e_Out_0_Vector4 = SAMPLE_TEXTURECUBE_LOD(_Property_7a236dc7b986418295c055925dd74f56_Out_0_Cubemap.tex, _Property_7a236dc7b986418295c055925dd74f56_Out_0_Cubemap.samplerstate, reflect(-IN.ObjectSpaceViewDirection, _RotateAboutAxis_18feb3536cc341de8dccef57e7646c24_Out_3_Vector3), float(0));
        float _Float_013baa7b5c7c409880b99e1afe6bffcf_Out_0_Float = float(6);
        float4 _Power_a1762f9654be457497b776e3eaabec5b_Out_2_Vector4;
        Unity_Power_float4(_SampleReflectedCubemap_1f499bf7f9484e52a24768d66256410e_Out_0_Vector4, (_Float_013baa7b5c7c409880b99e1afe6bffcf_Out_0_Float.xxxx), _Power_a1762f9654be457497b776e3eaabec5b_Out_2_Vector4);
        float _Float_e04f0f45684146e8a99d28233468eabc_Out_0_Float = float(0.5);
        float _Property_9b2c2d287d6444ab952da3ebe896b4c3_Out_0_Float = _Light_Pollution;
        float _Multiply_9c3dee328c674b5db172bd8114acb8b0_Out_2_Float;
        Unity_Multiply_float_float(_Property_9b2c2d287d6444ab952da3ebe896b4c3_Out_0_Float, 1.1, _Multiply_9c3dee328c674b5db172bd8114acb8b0_Out_2_Float);
        float _Clamp_a645348a153844c4a52147e7c49148ef_Out_3_Float;
        Unity_Clamp_float(_Multiply_9c3dee328c674b5db172bd8114acb8b0_Out_2_Float, float(0), float(1), _Clamp_a645348a153844c4a52147e7c49148ef_Out_3_Float);
        float _Lerp_b45be9d3aa3f456ebfd4109ddd645f32_Out_3_Float;
        Unity_Lerp_float(_Float_e04f0f45684146e8a99d28233468eabc_Out_0_Float, float(0), _Clamp_a645348a153844c4a52147e7c49148ef_Out_3_Float, _Lerp_b45be9d3aa3f456ebfd4109ddd645f32_Out_3_Float);
        float4 _Multiply_735b06ecba954052b597d2070edcc61c_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Power_a1762f9654be457497b776e3eaabec5b_Out_2_Vector4, (_Lerp_b45be9d3aa3f456ebfd4109ddd645f32_Out_3_Float.xxxx), _Multiply_735b06ecba954052b597d2070edcc61c_Out_2_Vector4);
        Bindings_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float _BottomMargin_64f1341362d54501b99283e28ee96c24;
        _BottomMargin_64f1341362d54501b99283e28ee96c24.WorldSpacePosition = IN.WorldSpacePosition;
        float _BottomMargin_64f1341362d54501b99283e28ee96c24_OutVector1_1_Float;
        SG_BottomMargin_d2ce96f6f60d9cd48beaec717d410f17_float(_BottomMargin_64f1341362d54501b99283e28ee96c24, _BottomMargin_64f1341362d54501b99283e28ee96c24_OutVector1_1_Float);
        float4 _Multiply_d408cb06455c4cb398283062217ba4e2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Multiply_735b06ecba954052b597d2070edcc61c_Out_2_Vector4, (_BottomMargin_64f1341362d54501b99283e28ee96c24_OutVector1_1_Float.xxxx), _Multiply_d408cb06455c4cb398283062217ba4e2_Out_2_Vector4);
        float3 _Maximum_2223a2cd9f6d48c9aa35ffdcd7090a87_Out_2_Vector3;
        Unity_Maximum_float3(_Multiply_4abce12d04e6472ebd0379bfaec50e20_Out_2_Vector3, (_Multiply_d408cb06455c4cb398283062217ba4e2_Out_2_Vector4.xyz), _Maximum_2223a2cd9f6d48c9aa35ffdcd7090a87_Out_2_Vector3);
        float4 _Property_71ee008a367d4150a8405aecd69e129a_Out_0_Vector4 = _Tint;
        float3 _Multiply_fbeeb451a4a341c7bf1aacad6cf2c1c8_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Maximum_2223a2cd9f6d48c9aa35ffdcd7090a87_Out_2_Vector3, (_Property_71ee008a367d4150a8405aecd69e129a_Out_0_Vector4.xyz), _Multiply_fbeeb451a4a341c7bf1aacad6cf2c1c8_Out_2_Vector3);
        OutVector3_1 = _Multiply_fbeeb451a4a341c7bf1aacad6cf2c1c8_Out_2_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTextureCube _Property_c4b1f102431c4de9b650909cd1666eb0_Out_0_Cubemap = UnityBuildTextureCubeStruct(_Starmap);
            float4 _Property_99b0b36085404ae78f8dcec7c5adcc1c_Out_0_Vector4 = _Sky_Color;
            float _Property_b604e6b76ec64065a61f579da44433c9_Out_0_Float = _Exposure;
            float4 _Property_09dc65a7df1b45219455f00f66e56201_Out_0_Vector4 = _Tint;
            float _Property_5cc7dd99c20d48babd3d3eed6c386df3_Out_0_Float = _Sun_Size;
            float _Property_4f3eb11bfbea4f0e9efafe3a80cb295b_Out_0_Float = _Light_Pollution;
            float _Property_6c39e23659804293b9afb889c2db97f4_Out_0_Float = _Haze_Strength;
            float _Property_c1f9f79b31f74124b57941edfa55c012_Out_0_Float = _Star_Roration_Speed;
            Bindings_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float _Sky_ce6521cb02ac4bddbc56ea4fda912eaa;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.ObjectSpaceNormal = IN.ObjectSpaceNormal;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.ObjectSpaceViewDirection = IN.ObjectSpaceViewDirection;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.WorldSpacePosition = IN.WorldSpacePosition;
            _Sky_ce6521cb02ac4bddbc56ea4fda912eaa.TimeParameters = IN.TimeParameters;
            float3 _Sky_ce6521cb02ac4bddbc56ea4fda912eaa_OutVector3_1_Vector3;
            SG_Sky_1d0bd3d7b57455e47b2390fca3645ff9_float(_Property_c4b1f102431c4de9b650909cd1666eb0_Out_0_Cubemap, _Property_99b0b36085404ae78f8dcec7c5adcc1c_Out_0_Vector4, _Property_b604e6b76ec64065a61f579da44433c9_Out_0_Float, _Property_09dc65a7df1b45219455f00f66e56201_Out_0_Vector4, _Property_5cc7dd99c20d48babd3d3eed6c386df3_Out_0_Float, float(0), _Property_4f3eb11bfbea4f0e9efafe3a80cb295b_Out_0_Float, _Property_6c39e23659804293b9afb889c2db97f4_Out_0_Float, NewGradient(0, 6, 2, float4(1, 1, 1, 0),float4(16, 0.5333333, 0, 0.4699931),float4(2.996078, 0.493884, 0, 0.5000076),float4(2.996078, 1.553205, 0, 0.6),float4(2.118547, 2.118547, 2.118547, 0.7000076),float4(1, 1, 1, 0.8),float4(0, 0, 0, 0),float4(0, 0, 0, 0), float2(1, 0),float2(1, 1),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0),float2(0, 0)), _Property_c1f9f79b31f74124b57941edfa55c012_Out_0_Float, _Sky_ce6521cb02ac4bddbc56ea4fda912eaa, _Sky_ce6521cb02ac4bddbc56ea4fda912eaa_OutVector3_1_Vector3);
            surface.BaseColor = _Sky_ce6521cb02ac4bddbc56ea4fda912eaa_OutVector3_1_Vector3;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
            output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
        
        
            output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
            output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitGBufferPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Sky_Color;
        float _Exposure;
        float4 _Tint;
        float _Sun_Size;
        float _Haze_Strength;
        float _Light_Pollution;
        float _Star_Roration_Speed;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURECUBE(_Starmap);
        SAMPLER(sampler_Starmap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Back
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Sky_Color;
        float _Exposure;
        float4 _Tint;
        float _Sun_Size;
        float _Haze_Strength;
        float _Light_Pollution;
        float _Star_Roration_Speed;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURECUBE(_Starmap);
        SAMPLER(sampler_Starmap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "UnityEditor.ShaderGraphUnlitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
    FallBack "Hidden/Shader Graph/FallbackError"
}