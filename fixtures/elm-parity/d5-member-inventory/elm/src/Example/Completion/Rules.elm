module Example.Completion.Rules exposing (..)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
m0 : Int -> Int
m0 p0 = p0
m1 : Int -> Int
m1 p0 = p0
m2 : Int -> Int
m2 p0 = p0
m3 : Int -> Int
m3 p0 = p0
m4 : Int -> Int -> Bool
m4 p0 p1 = p0 /= p1
m5 : Int -> Int -> Int
m5 p0 p1 = p0 + p1
m6 : Int -> Int -> Int
m6 p0 p1 = p0 - p1
m7 : Int -> Int -> Bool
m7 p0 p1 = p0 == p1
m8 : Int -> Int
m8 p0 = p0
m9 : Int -> Int
m9 p0 = p0
m10 : Int -> Int
m10 p0 = p0
m11 : Int -> Int
m11 p0 = p0
m12 : Int -> Int -> Bool
m12 p0 p1 = p0 /= p1
m13 : Int -> Int -> Bool
m13 p0 p1 = p0 == p1
m14 : Float -> Bool
m14 p0 = isInfinite p0
m15 : Float -> Bool
m15 p0 = isNaN p0
m16 : Float -> Float -> Bool
m16 p0 p1 = p0 /= p1
m17 : Float -> Float -> Bool
m17 p0 p1 = p0 < p1
m18 : Float -> Float -> Bool
m18 p0 p1 = p0 <= p1
m19 : Float -> Float -> Bool
m19 p0 p1 = p0 == p1
m20 : Float -> Float -> Bool
m20 p0 p1 = p0 > p1
m21 : Float -> Float -> Bool
m21 p0 p1 = p0 >= p1
m22 : Float -> Bool
m22 p0 = isInfinite p0
m23 : Float -> Bool
m23 p0 = isNaN p0
m24 : Float -> Float -> Bool
m24 p0 p1 = p0 /= p1
m25 : Float -> Float -> Bool
m25 p0 p1 = p0 < p1
m26 : Float -> Float -> Bool
m26 p0 p1 = p0 <= p1
m27 : Float -> Float -> Bool
m27 p0 p1 = p0 == p1
m28 : Float -> Float -> Bool
m28 p0 p1 = p0 > p1
m29 : Float -> Float -> Bool
m29 p0 p1 = p0 >= p1
m30 : Decimal -> Float
m30 p0 = Decimal.toFloat p0
m31 : Float -> Float
m31 p0 = p0
m32 : Float -> Float
m32 p0 = p0
m33 : Int -> Float
m33 p0 = toFloat p0
m34 : Int -> Float
m34 p0 = toFloat p0
m35 : Int -> Float
m35 p0 = toFloat p0
m36 : Int -> Float
m36 p0 = toFloat p0
m37 : Int -> Float
m37 p0 = toFloat p0
m38 : Int -> Float
m38 p0 = toFloat p0
m39 : Int -> Float
m39 p0 = toFloat p0
m40 : Int -> Float
m40 p0 = toFloat p0
m41 : Float -> Int
m41 p0 = truncate p0
m42 : Float -> Int
m42 p0 = truncate p0
m43 : Float -> Int
m43 p0 = truncate p0
m44 : Float -> Int
m44 p0 = truncate p0
m45 : Float -> Int
m45 p0 = truncate p0
m46 : Float -> Int
m46 p0 = truncate p0
m47 : Float -> Int
m47 p0 = truncate p0
m48 : Float -> Int
m48 p0 = truncate p0
m49 : Float -> Int
m49 p0 = truncate p0
m50 : Float -> Int
m50 p0 = truncate p0
m51 : Float -> Int
m51 p0 = truncate p0
m52 : Float -> Int
m52 p0 = truncate p0
m53 : Float -> Int
m53 p0 = truncate p0
m54 : Float -> Int
m54 p0 = truncate p0
m55 : Float -> Int
m55 p0 = truncate p0
m56 : Float -> Decimal
m56 p0 = Decimal.fromFloat p0
m57 : Float -> Float
m57 p0 = p0
m58 : Float -> Float
m58 p0 = p0
m59 : Float -> Int
m59 p0 = truncate p0
m60 : Float -> Int
m60 p0 = truncate p0
m61 : Float -> Int
m61 p0 = truncate p0
m62 : Float -> Int
m62 p0 = truncate p0
m63 : Float -> Int
m63 p0 = truncate p0
m64 : Float -> Int
m64 p0 = truncate p0
m65 : Float -> Int
m65 p0 = truncate p0
m66 : Float -> Int
m66 p0 = truncate p0
m67 : Float -> Int
m67 p0 = truncate p0
m68 : Int -> Float
m68 p0 = toFloat p0
m69 : Int -> Float
m69 p0 = toFloat p0
m70 : Float -> Float -> Bool
m70 p0 p1 = p0 /= p1
m71 : Float -> Float -> Float
m71 p0 p1 = p0 * p1
m72 : Float -> Float
m72 p0 = p0
m73 : Float -> Float -> Float
m73 p0 p1 = p0 + p1
m74 : Float -> Float
m74 p0 = -p0
m75 : Float -> Float -> Float
m75 p0 p1 = p0 - p1
m76 : Float -> Float -> Float
m76 p0 p1 = p0 / p1
m77 : Float -> Float -> Bool
m77 p0 p1 = p0 < p1
m78 : Float -> Float -> Bool
m78 p0 p1 = p0 <= p1
m79 : Float -> Float -> Bool
m79 p0 p1 = p0 == p1
m80 : Float -> Float -> Bool
m80 p0 p1 = p0 > p1
m81 : Float -> Float -> Bool
m81 p0 p1 = p0 >= p1
m82 : Int -> Float
m82 p0 = toFloat p0
m83 : Float -> Int
m83 p0 = truncate p0
m84 : Float -> Int
m84 p0 = truncate p0
m85 : Int -> Int
m85 p0 = p0
m86 : Int -> Int
m86 p0 = p0
m87 : Int -> Char
m87 p0 = Char.fromCode p0
m88 : Float -> Int
m88 p0 = truncate p0
m89 : Float -> Int
m89 p0 = truncate p0
m90 : Int -> Int
m90 p0 = p0
m91 : Int -> Int
m91 p0 = p0
m92 : Int -> Char
m92 p0 = Char.fromCode p0
m93 : Int -> Int
m93 p0 = p0
m94 : Int -> Int
m94 p0 = p0
m95 : Int -> Int
m95 p0 = p0
m96 : Int -> Int
m96 p0 = p0
m97 : Int -> Int
m97 p0 = p0
m98 : Int -> Int
m98 p0 = p0
m99 : Int -> Int
m99 p0 = p0
m100 : Int -> Int
m100 p0 = p0
m101 : Int -> Int
m101 p0 = p0
m102 : Int -> Decimal
m102 p0 = Decimal.fromInt p0
m103 : Int -> Float
m103 p0 = toFloat p0
m104 : Int -> Float
m104 p0 = toFloat p0
m105 : Int -> Int
m105 p0 = p0
m106 : Int -> Int
m106 p0 = p0
m107 : Int -> Int
m107 p0 = p0
m108 : Int -> Int
m108 p0 = p0
m109 : Int -> Int
m109 p0 = p0
m110 : Int -> Int
m110 p0 = p0
m111 : Int -> Int
m111 p0 = p0
m112 : Int -> Int
m112 p0 = p0
m113 : Int -> Int
m113 p0 = p0
m114 : Int -> Int
m114 p0 = p0
m115 : Char -> Int
m115 p0 = Char.toCode p0
m116 : Int -> Int
m116 p0 = p0
m117 : Int -> Int
m117 p0 = p0
m118 : Int -> Int
m118 p0 = p0
m119 : Int -> Int
m119 p0 = p0
m120 : Int -> Int
m120 p0 = p0
m121 : Int -> Int
m121 p0 = p0
m122 : Int -> Int
m122 p0 = p0
m123 : Int -> Int
m123 p0 = p0
m124 : Int -> Int
m124 p0 = p0
m125 : Int -> Int -> Bool
m125 p0 p1 = p0 /= p1
m126 : Int -> Int -> Int
m126 p0 p1 = remainderBy p1 p0
m127 : Int -> Int -> Int
m127 p0 p1 = p0 * p1
m128 : Int -> Int
m128 p0 = p0
m129 : Int -> Int -> Int
m129 p0 p1 = p0 + p1
m130 : Int -> Int
m130 p0 = -p0
m131 : Int -> Int -> Int
m131 p0 p1 = p0 - p1
m132 : Int -> Int -> Int
m132 p0 p1 = p0 // p1
m133 : Int -> Int -> Bool
m133 p0 p1 = p0 < p1
m134 : Int -> Int -> Bool
m134 p0 p1 = p0 <= p1
m135 : Int -> Int -> Bool
m135 p0 p1 = p0 == p1
m136 : Int -> Int -> Bool
m136 p0 p1 = p0 > p1
m137 : Int -> Int -> Bool
m137 p0 p1 = p0 >= p1
m138 : Int -> Int -> Int
m138 p0 p1 = p0 * p1
m139 : Int -> Int -> Int
m139 p0 p1 = p0 + p1
m140 : Int -> Int
m140 p0 = -p0
m141 : Int -> Int -> Int
m141 p0 p1 = p0 - p1
m142 : Int -> Int -> Int
m142 p0 p1 = p0 // p1
m143 : Int -> Float
m143 p0 = toFloat p0
m144 : Int -> Int
m144 p0 = p0
m145 : Float -> Int
m145 p0 = truncate p0
m146 : Float -> Int
m146 p0 = truncate p0
m147 : Int -> Int
m147 p0 = p0
m148 : Int -> Int
m148 p0 = p0
m149 : Int -> Int
m149 p0 = p0
m150 : Int -> Int
m150 p0 = p0
m151 : Int -> Int
m151 p0 = p0
m152 : Int -> Int
m152 p0 = p0
m153 : Int -> Char
m153 p0 = Char.fromCode p0
m154 : Int -> Int
m154 p0 = p0
m155 : Float -> Int
m155 p0 = truncate p0
m156 : Float -> Int
m156 p0 = truncate p0
m157 : Int -> Int
m157 p0 = p0
m158 : Int -> Int
m158 p0 = p0
m159 : Int -> Int
m159 p0 = p0
m160 : Int -> Int
m160 p0 = p0
m161 : Int -> Int
m161 p0 = p0
m162 : Int -> Int
m162 p0 = p0
m163 : Int -> Char
m163 p0 = Char.fromCode p0
m164 : Int -> Int
m164 p0 = p0
m165 : Int -> Int
m165 p0 = p0
m166 : Int -> Int
m166 p0 = p0
m167 : Int -> Int
m167 p0 = p0
m168 : Int -> Int
m168 p0 = p0
m169 : Int -> Int
m169 p0 = p0
m170 : Int -> Int
m170 p0 = p0
m171 : Int -> Int
m171 p0 = p0
m172 : Int -> Int
m172 p0 = p0
m173 : Int -> Decimal
m173 p0 = Decimal.fromInt p0
m174 : Int -> Float
m174 p0 = toFloat p0
m175 : Int -> Float
m175 p0 = toFloat p0
m176 : Int -> Int
m176 p0 = p0
m177 : Int -> Int
m177 p0 = p0
m178 : Int -> Int
m178 p0 = p0
m179 : Int -> Int
m179 p0 = p0
m180 : Int -> Int
m180 p0 = p0
m181 : Int -> Int
m181 p0 = p0
m182 : Int -> Int
m182 p0 = p0
m183 : Int -> Int
m183 p0 = p0
m184 : Int -> Int
m184 p0 = p0
m185 : Int -> Int
m185 p0 = p0
m186 : Char -> Int
m186 p0 = Char.toCode p0
m187 : Int -> Int
m187 p0 = p0
m188 : Int -> Int
m188 p0 = p0
m189 : Int -> Int
m189 p0 = p0
m190 : Int -> Int
m190 p0 = p0
m191 : Int -> Int -> Bool
m191 p0 p1 = p0 /= p1
m192 : Int -> Int -> Int
m192 p0 p1 = remainderBy p1 p0
m193 : Int -> Int -> Int
m193 p0 p1 = p0 * p1
m194 : Int -> Int
m194 p0 = p0
m195 : Int -> Int -> Int
m195 p0 p1 = p0 + p1
m196 : Int -> Int
m196 p0 = -p0
m197 : Int -> Int -> Int
m197 p0 p1 = p0 - p1
m198 : Int -> Int -> Int
m198 p0 p1 = p0 // p1
m199 : Int -> Int -> Bool
m199 p0 p1 = p0 < p1
m200 : Int -> Int -> Bool
m200 p0 p1 = p0 <= p1
m201 : Int -> Int -> Bool
m201 p0 p1 = p0 == p1
m202 : Int -> Int -> Bool
m202 p0 p1 = p0 > p1
m203 : Int -> Int -> Bool
m203 p0 p1 = p0 >= p1
m204 : Int -> Int -> Int
m204 p0 p1 = p0 * p1
m205 : Int -> Int -> Int
m205 p0 p1 = p0 + p1
m206 : Int -> Int
m206 p0 = -p0
m207 : Int -> Int -> Int
m207 p0 p1 = p0 - p1
m208 : Int -> Int -> Int
m208 p0 p1 = p0 // p1
m209 : Int -> Float
m209 p0 = toFloat p0
m210 : Int -> Int
m210 p0 = p0
m211 : Float -> Int
m211 p0 = truncate p0
m212 : Float -> Int
m212 p0 = truncate p0
m213 : Float -> Int
m213 p0 = truncate p0
m214 : Int -> Int
m214 p0 = p0
m215 : Int -> Int
m215 p0 = p0
m216 : Int -> Char
m216 p0 = Char.fromCode p0
m217 : Int -> Decimal
m217 p0 = Decimal.fromInt p0
m218 : Int -> Float
m218 p0 = toFloat p0
m219 : Int -> Float
m219 p0 = toFloat p0
m220 : Int -> Int
m220 p0 = p0
m221 : Int -> Int
m221 p0 = p0
m222 : Int -> Int
m222 p0 = p0
m223 : Int -> Int
m223 p0 = p0
m224 : Int -> Int
m224 p0 = p0
m225 : Int -> Int
m225 p0 = p0
m226 : Int -> Int
m226 p0 = p0
m227 : Int -> Int
m227 p0 = p0
m228 : Int -> Int
m228 p0 = p0
m229 : Int -> Int
m229 p0 = p0
m230 : Int -> Int
m230 p0 = p0
m231 : Int -> Int
m231 p0 = p0
m232 : Char -> Int
m232 p0 = Char.toCode p0
m233 : Int -> Int
m233 p0 = p0
m234 : Int -> Int
m234 p0 = p0
m235 : Int -> Int
m235 p0 = p0
m236 : Int -> Int
m236 p0 = p0
m237 : Int -> Int
m237 p0 = p0
m238 : Int -> Int
m238 p0 = p0
m239 : Int -> Int
m239 p0 = p0
m240 : Int -> Int
m240 p0 = p0
m241 : Int -> Int
m241 p0 = p0
m242 : Int -> Int -> Bool
m242 p0 p1 = p0 /= p1
m243 : Int -> Int -> Bool
m243 p0 p1 = p0 /= p1
m244 : Int -> Int -> Bool
m244 p0 p1 = p0 /= p1
m245 : Int -> Int -> Bool
m245 p0 p1 = p0 /= p1
m246 : Int -> Int -> Bool
m246 p0 p1 = p0 /= p1
m247 : Int -> Int -> Int
m247 p0 p1 = remainderBy p1 p0
m248 : Int -> Int -> Int
m248 p0 p1 = p0 * p1
m249 : Int -> Int
m249 p0 = p0
m250 : Int -> Int -> Int
m250 p0 p1 = p0 + p1
m251 : Int -> Int
m251 p0 = -p0
m252 : Int -> Int -> Int
m252 p0 p1 = p0 - p1
m253 : Int -> Int -> Int
m253 p0 p1 = p0 // p1
m254 : Int -> Int -> Bool
m254 p0 p1 = p0 < p1
m255 : Int -> Int -> Bool
m255 p0 p1 = p0 < p1
m256 : Int -> Int -> Bool
m256 p0 p1 = p0 < p1
m257 : Int -> Int -> Bool
m257 p0 p1 = p0 < p1
m258 : Int -> Int -> Bool
m258 p0 p1 = p0 < p1
m259 : Int -> Int -> Bool
m259 p0 p1 = p0 <= p1
m260 : Int -> Int -> Bool
m260 p0 p1 = p0 <= p1
m261 : Int -> Int -> Bool
m261 p0 p1 = p0 <= p1
m262 : Int -> Int -> Bool
m262 p0 p1 = p0 <= p1
m263 : Int -> Int -> Bool
m263 p0 p1 = p0 <= p1
m264 : Int -> Int -> Bool
m264 p0 p1 = p0 == p1
m265 : Int -> Int -> Bool
m265 p0 p1 = p0 == p1
m266 : Int -> Int -> Bool
m266 p0 p1 = p0 == p1
m267 : Int -> Int -> Bool
m267 p0 p1 = p0 == p1
m268 : Int -> Int -> Bool
m268 p0 p1 = p0 == p1
m269 : Int -> Int -> Bool
m269 p0 p1 = p0 > p1
m270 : Int -> Int -> Bool
m270 p0 p1 = p0 > p1
m271 : Int -> Int -> Bool
m271 p0 p1 = p0 > p1
m272 : Int -> Int -> Bool
m272 p0 p1 = p0 > p1
m273 : Int -> Int -> Bool
m273 p0 p1 = p0 > p1
m274 : Int -> Int -> Bool
m274 p0 p1 = p0 >= p1
m275 : Int -> Int -> Bool
m275 p0 p1 = p0 >= p1
m276 : Int -> Int -> Bool
m276 p0 p1 = p0 >= p1
m277 : Int -> Int -> Bool
m277 p0 p1 = p0 >= p1
m278 : Int -> Int -> Bool
m278 p0 p1 = p0 >= p1
