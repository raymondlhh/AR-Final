// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Dypsloom/Grass"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_SpecColor("Specular Color",Color)=(1,1,1,1)
		_Strength("Strength", Float) = 0.3
		_NoiseScale("NoiseScale", Float) = 1.04
		_NoiseOffset("NoiseOffset", Vector) = (6,0,0,0)
		_Opacity("Opacity", 2D) = "white" {}
		_EmissiveTexture("EmissiveTexture", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		AlphaToMask On
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 5.0
		#pragma surface surf BlinnPhong keepalpha noshadow exclude_path:deferred vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float2 _NoiseOffset;
		uniform float _NoiseScale;
		uniform float _Strength;
		uniform sampler2D _EmissiveTexture;
		uniform float4 _EmissiveTexture_ST;
		uniform sampler2D _Opacity;
		uniform float4 _Opacity_ST;
		uniform float _Cutoff = 0.5;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float simplePerlin2D15 = snoise( (ase_vertex3Pos*1.0 + float3( ( _Time.y * _NoiseOffset ) ,  0.0 )).xy*_NoiseScale );
			simplePerlin2D15 = simplePerlin2D15*0.5 + 0.5;
			float temp_output_25_0 = ( ( simplePerlin2D15 - 0.5 ) * _Strength );
			float3 break27 = ase_vertex3Pos;
			float4 appendResult29 = (float4(( temp_output_25_0 + break27.x ) , break27.y , ( temp_output_25_0 + break27.z ) , 0.0));
			float4 lerpResult55 = lerp( float4( ase_vertex3Pos , 0.0 ) , appendResult29 , v.texcoord.xy.y);
			v.vertex.xyz += ( float4( ase_vertex3Pos , 0.0 ) - lerpResult55 ).xyz;
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_EmissiveTexture = i.uv_texcoord * _EmissiveTexture_ST.xy + _EmissiveTexture_ST.zw;
			o.Emission = tex2D( _EmissiveTexture, uv_EmissiveTexture ).rgb;
			o.Alpha = 1;
			float2 uv_Opacity = i.uv_texcoord * _Opacity_ST.xy + _Opacity_ST.zw;
			clip( tex2D( _Opacity, uv_Opacity ).r - _Cutoff );
		}

		ENDCG
	}
}
/*ASEBEGIN
Version=17800
-2556;27;2546;1347;2714.727;1386.744;1.827094;True;True
Node;AmplifyShaderEditor.SimpleTimeNode;12;-1759.418,-464.4632;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;13;-1757.418,-383.0633;Inherit;False;Property;_NoiseOffset;NoiseOffset;4;0;Create;True;0;0;False;0;6,0;1.31,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PosVertexDataNode;10;-1761.044,-615.939;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-1588.419,-403.4633;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;11;-1474.622,-616.43;Inherit;True;3;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-1497.193,-773.879;Inherit;False;Property;_NoiseScale;NoiseScale;3;0;Create;True;0;0;False;0;1.04;1.45;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;15;-1272.072,-878.3021;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0.52,0;False;1;FLOAT;6.14;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;26;-1224.833,-396.0222;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;23;-1066.585,-630.1547;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-1090.583,-514.1295;Inherit;False;Property;_Strength;Strength;2;0;Create;True;0;0;False;0;0.3;0.74;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;27;-923.1965,-392.0914;Inherit;True;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-917.5095,-609.2071;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;28;-683.1783,-606.1738;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;76;-693.2808,-164.8139;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;53;-472.2202,-112.6893;Inherit;True;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PosVertexDataNode;56;-308.6886,-641.6961;Inherit;True;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;54;-235.9156,-221.3618;Inherit;True;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.DynamicAppendNode;29;-483.1681,-397.1738;Inherit;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;55;50.42469,-341.9177;Inherit;True;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;75;-74.6713,-28.61786;Inherit;True;Property;_EmissiveTexture;EmissiveTexture;6;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;77;354.2874,-417.4182;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;74;-51.81842,191.4568;Inherit;True;Property;_Opacity;Opacity;5;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;792.1245,-118.7947;Float;False;True;-1;7;;0;0;BlinnPhong;Dypsloom/Grass;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;False;0;False;TransparentCutout;;AlphaTest;ForwardOnly;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;True;0;0;False;-1;1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;14;0;12;0
WireConnection;14;1;13;0
WireConnection;11;0;10;0
WireConnection;11;2;14;0
WireConnection;15;0;11;0
WireConnection;15;1;22;0
WireConnection;23;0;15;0
WireConnection;27;0;26;0
WireConnection;25;0;23;0
WireConnection;25;1;24;0
WireConnection;28;0;25;0
WireConnection;28;1;27;0
WireConnection;76;0;25;0
WireConnection;76;1;27;2
WireConnection;54;0;53;0
WireConnection;29;0;28;0
WireConnection;29;1;27;1
WireConnection;29;2;76;0
WireConnection;55;0;56;0
WireConnection;55;1;29;0
WireConnection;55;2;54;1
WireConnection;77;0;56;0
WireConnection;77;1;55;0
WireConnection;0;2;75;0
WireConnection;0;10;74;0
WireConnection;0;11;77;0
ASEEND*/
//CHKSM=DD4B08BC41CFAE2375E5103DA3022BE0AF56F02C