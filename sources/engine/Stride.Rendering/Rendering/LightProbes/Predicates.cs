// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Original source code license :
/*****************************************************************************/
/*                                                                           */
/*  Routines for Arbitrary Precision Floating-point Arithmetic               */
/*  and Fast Robust Geometric Predicates                                     */
/*  (predicates.c)                                                           */
/*                                                                           */
/*  May 18, 1996                                                             */
/*                                                                           */
/*  Placed in the public domain by                                           */
/*  Jonathan Richard Shewchuk                                                */
/*  School of Computer Science                                               */
/*  Carnegie Mellon University                                               */
/*  5000 Forbes Avenue                                                       */
/*  Pittsburgh, Pennsylvania  15213-3891                                     */
/*  jrs@cs.cmu.edu                                                           */
/*                                                                           */
/*  This file contains C implementation of algorithms for exact addition     */
/*    and multiplication of floating-point numbers, and predicates for       */
/*    robustly performing the orientation and incircle tests used in         */
/*    computational geometry.  The algorithms and underlying theory are      */
/*    described in Jonathan Richard Shewchuk.  "Adaptive Precision Floating- */
/*    Point Arithmetic and Fast Robust Geometric Predicates."  Technical     */
/*    Report CMU-CS-96-140, School of Computer Science, Carnegie Mellon      */
/*    University, Pittsburgh, Pennsylvania, May 1996.  (Submitted to         */
/*    Discrete & Computational Geometry.)                                    */
/*                                                                           */
/*  This file, the paper listed above, and other information are available   */
/*    from the Web page http://www.cs.cmu.edu/~quake/robust.html .           */
/*                                                                           */
/*****************************************************************************/

using System;

using Stride.Core.Mathematics;

namespace Stride.Rendering.LightProbes;

internal sealed class Predicates
{
    private readonly float splitter;
    private readonly float resulterrbound;
    private readonly float o3derrboundA;
    private readonly float o3derrboundB;
    private readonly float o3derrboundC;
    private readonly float isperrboundA;
    private readonly float isperrboundB;
    private readonly float isperrboundC;

    public Predicates()
    {
        float lastcheck;

        bool everyOther = true;
        float half = 0.5f;
        float epsilon = 1.0f;
        splitter = 1.0f;
        float check = 1.0f;
        /* Repeatedly divide `epsilon' by two until it is too small to add to    */
        /*   one without causing roundoff.  (Also check if the sum is equal to   */
        /*   the previous sum, for machines that round up instead of using exact */
        /*   rounding.  Not that this library will work on such machines anyway. */
        do {
            lastcheck = check;
            epsilon *= half;
            if (everyOther) {
                splitter *= 2.0f;
            }
            everyOther = !everyOther;
            check = 1.0f + epsilon;
        } while ((check != 1.0f) && (check != lastcheck));
        splitter += 1.0f;

        /* Error bounds for orientation and incircle tests. */
        resulterrbound = (3.0f + 8.0f * epsilon) * epsilon;
        o3derrboundA = (7.0f + 56.0f * epsilon) * epsilon;
        o3derrboundB = (3.0f + 28.0f * epsilon) * epsilon;
        o3derrboundC = (26.0f + 288.0f * epsilon) * epsilon * epsilon;
        isperrboundA = (16.0f + 224.0f * epsilon) * epsilon;
        isperrboundB = (5.0f + 72.0f * epsilon) * epsilon;
        isperrboundC = (71.0f + 1408.0f * epsilon) * epsilon * epsilon;
    }

    public float Orient3d(ref Vector3 pa, ref Vector3 pb, ref Vector3 pc, ref Vector3 pd)
    {
        float adx = pa[0] - pd[0];
        float bdx = pb[0] - pd[0];
        float cdx = pc[0] - pd[0];
        float ady = pa[1] - pd[1];
        float bdy = pb[1] - pd[1];
        float cdy = pc[1] - pd[1];
        float adz = pa[2] - pd[2];
        float bdz = pb[2] - pd[2];
        float cdz = pc[2] - pd[2];

        float bdxcdy = bdx * cdy;
        float cdxbdy = cdx * bdy;

        float cdxady = cdx * ady;
        float adxcdy = adx * cdy;

        float adxbdy = adx * bdy;
        float bdxady = bdx * ady;

        float det = adz * (bdxcdy - cdxbdy)
                    + bdz * (cdxady - adxcdy)
                    + cdz * (adxbdy - bdxady);

        float permanent = (Math.Abs(bdxcdy) + Math.Abs(cdxbdy)) * Math.Abs(adz)
                          + (Math.Abs(cdxady) + Math.Abs(adxcdy)) * Math.Abs(bdz)
                          + (Math.Abs(adxbdy) + Math.Abs(bdxady)) * Math.Abs(cdz);
        float errbound = o3derrboundA * permanent;
        if ((det > errbound) || (-det > errbound)) {
            return det;
        }

        return Orient3dAdapt(pa, pb, pc, pd, permanent);
    }

    private float Orient3dAdapt(Vector3 pa, Vector3 pb, Vector3 pc, Vector3 pd, float permanent)
    {
        Span<float> bc = stackalloc float[4];
        Span<float> ca = stackalloc float[4];
        Span<float> ab = stackalloc float[4];
        Span<float> adet = stackalloc float[8];
        Span<float> bdet = stackalloc float[8];
        Span<float> cdet = stackalloc float[8];
        Span<float> abdet = stackalloc float[16];
        
        Span<float> fin1 = stackalloc float[192];
        Span<float> fin2 = stackalloc float[192];

        float at_blarge, at_clarge;
	    float bt_clarge, bt_alarge;
	    float ct_alarge, ct_blarge;
        Span<float> at_b = stackalloc float[4];
        Span<float> at_c = stackalloc float[4];
        Span<float> bt_c = stackalloc float[4];
        Span<float> bt_a = stackalloc float[4];
        Span<float> ct_a = stackalloc float[4];
        Span<float> ct_b = stackalloc float[4];
        int at_blen, at_clen, bt_clen, bt_alen, ct_alen, ct_blen;
        Span<float> bct = stackalloc float[8];
        Span<float> cat = stackalloc float[8];
        Span<float> abt = stackalloc float[8];
        Span<float> u = stackalloc float[4];
        Span<float> v = stackalloc float[12];
        Span<float> w = stackalloc float[16];
        float u3;
	    int vlength;
	    float negate;

	    float adx = pa[0] - pd[0];
	    float bdx = pb[0] - pd[0];
	    float cdx = pc[0] - pd[0];
	    float ady = pa[1] - pd[1];
	    float bdy = pb[1] - pd[1];
	    float cdy = pc[1] - pd[1];
	    float adz = pa[2] - pd[2];
	    float bdz = pb[2] - pd[2];
	    float cdz = pc[2] - pd[2];

	    Two_Product(bdx, cdy, out var bdxcdy1, out var bdxcdy0);
	    Two_Product(cdx, bdy, out var cdxbdy1, out var cdxbdy0);
	    Two_Two_Diff(bdxcdy1, bdxcdy0, cdxbdy1, cdxbdy0, out var bc3, out bc[2], out bc[1], out bc[0]);
	    bc[3] = bc3;
	    int alen = scale_expansion_zeroelim(4, bc, adz, adet);

	    Two_Product(cdx, ady, out var cdxady1, out var cdxady0);
	    Two_Product(adx, cdy, out var adxcdy1, out var adxcdy0);
	    Two_Two_Diff(cdxady1, cdxady0, adxcdy1, adxcdy0, out var ca3, out ca[2], out ca[1], out ca[0]);
	    ca[3] = ca3;
	    int blen = scale_expansion_zeroelim(4, ca, bdz, bdet);

	    Two_Product(adx, bdy, out var adxbdy1, out var adxbdy0);
	    Two_Product(bdx, ady, out var bdxady1, out var bdxady0);
	    Two_Two_Diff(adxbdy1, adxbdy0, bdxady1, bdxady0, out var ab3, out ab[2], out ab[1], out ab[0]);
	    ab[3] = ab3;
	    int clen = scale_expansion_zeroelim(4, ab, cdz, cdet);

	    int ablen = fast_expansion_sum_zeroelim(alen, adet, blen, bdet, abdet);
	    int finlength = fast_expansion_sum_zeroelim(ablen, abdet, clen, cdet, fin1);

	    float det = estimate(finlength, fin1);
	    float errbound = o3derrboundB * permanent;
	    if ((det >= errbound) || (-det >= errbound)) {
		    return det;
	    }

	    Two_Diff_Tail(pa[0], pd[0], adx, out var adxtail);
	    Two_Diff_Tail(pb[0], pd[0], bdx, out var bdxtail);
	    Two_Diff_Tail(pc[0], pd[0], cdx, out var cdxtail);
	    Two_Diff_Tail(pa[1], pd[1], ady, out var adytail);
	    Two_Diff_Tail(pb[1], pd[1], bdy, out var bdytail);
	    Two_Diff_Tail(pc[1], pd[1], cdy, out var cdytail);
	    Two_Diff_Tail(pa[2], pd[2], adz, out var adztail);
	    Two_Diff_Tail(pb[2], pd[2], bdz, out var bdztail);
	    Two_Diff_Tail(pc[2], pd[2], cdz, out var cdztail);

	    if ((adxtail == 0f) && (bdxtail == 0f) && (cdxtail == 0f)
		    && (adytail == 0f) && (bdytail == 0f) && (cdytail == 0f)
		    && (adztail == 0f) && (bdztail == 0f) && (cdztail == 0f)) {
		    return det;
	    }

	    errbound = o3derrboundC * permanent + resulterrbound * Math.Abs(det);
	    det += (adz * ((bdx * cdytail + cdy * bdxtail)
		    - (bdy * cdxtail + cdx * bdytail))
		    + adztail * (bdx * cdy - bdy * cdx))
		    + (bdz * ((cdx * adytail + ady * cdxtail)
		    - (cdy * adxtail + adx * cdytail))
		    + bdztail * (cdx * ady - cdy * adx))
		    + (cdz * ((adx * bdytail + bdy * adxtail)
		    - (ady * bdxtail + bdx * adytail))
		    + cdztail * (adx * bdy - ady * bdx));
	    if ((det >= errbound) || (-det >= errbound)) {
		    return det;
	    }

	    Span<float> finnow = fin1;
	    Span<float> finother = fin2;

	    if (adxtail == 0f) {
		    if (adytail == 0f) {
			    at_b[0] = 0.0f;
			    at_blen = 1;
			    at_c[0] = 0.0f;
			    at_clen = 1;
		    }
		    else {
			    negate = -adytail;
			    Two_Product(negate, bdx, out at_blarge, out at_b[0]);
			    at_b[1] = at_blarge;
			    at_blen = 2;
			    Two_Product(adytail, cdx, out at_clarge, out at_c[0]);
			    at_c[1] = at_clarge;
			    at_clen = 2;
		    }
	    }
	    else {
		    if (adytail == 0f) {
			    Two_Product(adxtail, bdy, out at_blarge, out at_b[0]);
			    at_b[1] = at_blarge;
			    at_blen = 2;
			    negate = -adxtail;
			    Two_Product(negate, cdy, out at_clarge, out at_c[0]);
			    at_c[1] = at_clarge;
			    at_clen = 2;
		    }
		    else {
			    Two_Product(adxtail, bdy, out var adxt_bdy1, out var adxt_bdy0);
			    Two_Product(adytail, bdx, out var adyt_bdx1, out var adyt_bdx0);
			    Two_Two_Diff(adxt_bdy1, adxt_bdy0, adyt_bdx1, adyt_bdx0,
				    out at_blarge, out at_b[2], out at_b[1], out at_b[0]);
			    at_b[3] = at_blarge;
			    at_blen = 4;
			    Two_Product(adytail, cdx, out var adyt_cdx1, out var adyt_cdx0);
			    Two_Product(adxtail, cdy, out var adxt_cdy1, out var adxt_cdy0);
			    Two_Two_Diff(adyt_cdx1, adyt_cdx0, adxt_cdy1, adxt_cdy0,
				    out at_clarge, out at_c[2], out at_c[1], out at_c[0]);
			    at_c[3] = at_clarge;
			    at_clen = 4;
		    }
	    }
	    if (bdxtail == 0f) {
		    if (bdytail == 0f) {
			    bt_c[0] = 0.0f;
			    bt_clen = 1;
			    bt_a[0] = 0.0f;
			    bt_alen = 1;
		    }
		    else {
			    negate = -bdytail;
			    Two_Product(negate, cdx, out bt_clarge, out bt_c[0]);
			    bt_c[1] = bt_clarge;
			    bt_clen = 2;
			    Two_Product(bdytail, adx, out bt_alarge, out bt_a[0]);
			    bt_a[1] = bt_alarge;
			    bt_alen = 2;
		    }
	    }
	    else {
		    if (bdytail == 0f) {
			    Two_Product(bdxtail, cdy, out bt_clarge, out bt_c[0]);
			    bt_c[1] = bt_clarge;
			    bt_clen = 2;
			    negate = -bdxtail;
			    Two_Product(negate, ady, out bt_alarge, out bt_a[0]);
			    bt_a[1] = bt_alarge;
			    bt_alen = 2;
		    }
		    else {
			    Two_Product(bdxtail, cdy, out var bdxt_cdy1, out var bdxt_cdy0);
			    Two_Product(bdytail, cdx, out var bdyt_cdx1, out var bdyt_cdx0);
			    Two_Two_Diff(bdxt_cdy1, bdxt_cdy0, bdyt_cdx1, bdyt_cdx0, out bt_clarge, out bt_c[2], out bt_c[1], out bt_c[0]);
			    bt_c[3] = bt_clarge;
			    bt_clen = 4;
			    Two_Product(bdytail, adx, out var bdyt_adx1, out var bdyt_adx0);
			    Two_Product(bdxtail, ady, out var bdxt_ady1, out var bdxt_ady0);
			    Two_Two_Diff(bdyt_adx1, bdyt_adx0, bdxt_ady1, bdxt_ady0, out bt_alarge, out bt_a[2], out bt_a[1], out bt_a[0]);
			    bt_a[3] = bt_alarge;
			    bt_alen = 4;
		    }
	    }
	    if (cdxtail == 0f) {
		    if (cdytail == 0f) {
			    ct_a[0] = 0.0f;
			    ct_alen = 1;
			    ct_b[0] = 0.0f;
			    ct_blen = 1;
		    }
		    else {
			    negate = -cdytail;
			    Two_Product(negate, adx, out ct_alarge, out ct_a[0]);
			    ct_a[1] = ct_alarge;
			    ct_alen = 2;
			    Two_Product(cdytail, bdx, out ct_blarge, out ct_b[0]);
			    ct_b[1] = ct_blarge;
			    ct_blen = 2;
		    }
	    }
	    else {
		    if (cdytail == 0f) {
			    Two_Product(cdxtail, ady, out ct_alarge, out ct_a[0]);
			    ct_a[1] = ct_alarge;
			    ct_alen = 2;
			    negate = -cdxtail;
			    Two_Product(negate, bdy, out ct_blarge, out ct_b[0]);
			    ct_b[1] = ct_blarge;
			    ct_blen = 2;
		    }
		    else {
			    Two_Product(cdxtail, ady, out var cdxt_ady1, out var cdxt_ady0);
			    Two_Product(cdytail, adx, out var cdyt_adx1, out var cdyt_adx0);
			    Two_Two_Diff(cdxt_ady1, cdxt_ady0, cdyt_adx1, cdyt_adx0, out ct_alarge, out ct_a[2], out ct_a[1], out ct_a[0]);
			    ct_a[3] = ct_alarge;
			    ct_alen = 4;
			    Two_Product(cdytail, bdx, out var cdyt_bdx1, out var cdyt_bdx0);
			    Two_Product(cdxtail, bdy, out var cdxt_bdy1, out var cdxt_bdy0);
			    Two_Two_Diff(cdyt_bdx1, cdyt_bdx0, cdxt_bdy1, cdxt_bdy0, out ct_blarge, out ct_b[2], out ct_b[1], out ct_b[0]);
			    ct_b[3] = ct_blarge;
			    ct_blen = 4;
		    }
	    }

	    int bctlen = fast_expansion_sum_zeroelim(bt_clen, bt_c, ct_blen, ct_b, bct);
	    int wlength = scale_expansion_zeroelim(bctlen, bct, adz, w);
	    finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
		    finother);
	    
        Span<float> finswap = finnow; finnow = finother; finother = finswap;

	    int catlen = fast_expansion_sum_zeroelim(ct_alen, ct_a, at_clen, at_c, cat);
	    wlength = scale_expansion_zeroelim(catlen, cat, bdz, w);
	    finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
		    finother);
	    finswap = finnow; finnow = finother; finother = finswap;

	    int abtlen = fast_expansion_sum_zeroelim(at_blen, at_b, bt_alen, bt_a, abt);
	    wlength = scale_expansion_zeroelim(abtlen, abt, cdz, w);
	    finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
		    finother);
	    finswap = finnow; finnow = finother; finother = finswap;

	    if (adztail != 0f) {
		    vlength = scale_expansion_zeroelim(4, bc, adztail, v);
		    finlength = fast_expansion_sum_zeroelim(finlength, finnow, vlength, v,
			    finother);
		    finswap = finnow; finnow = finother; finother = finswap;
	    }
	    if (bdztail != 0f) {
		    vlength = scale_expansion_zeroelim(4, ca, bdztail, v);
		    finlength = fast_expansion_sum_zeroelim(finlength, finnow, vlength, v,
			    finother);
		    finswap = finnow; finnow = finother; finother = finswap;
	    }
	    if (cdztail != 0f) {
		    vlength = scale_expansion_zeroelim(4, ab, cdztail, v);
		    finlength = fast_expansion_sum_zeroelim(finlength, finnow, vlength, v,
			    finother);
		    finswap = finnow; finnow = finother; finother = finswap;
	    }

	    if (adxtail != 0f) {
		    if (bdytail != 0f) {
			    Two_Product(adxtail, bdytail, out var adxt_bdyt1, out var adxt_bdyt0);
			    Two_One_Product(adxt_bdyt1, adxt_bdyt0, cdz, out u3, out u[2], out u[1], out u[0]);
			    u[3] = u3;
			    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				    finother);
			    finswap = finnow; finnow = finother; finother = finswap;
			    if (cdztail != 0f) {
				    Two_One_Product(adxt_bdyt1, adxt_bdyt0, cdztail, out u3, out u[2], out u[1], out u[0]);
				    u[3] = u3;
				    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					    finother);
				    finswap = finnow; finnow = finother; finother = finswap;
			    }
		    }
		    if (cdytail != 0f) {
			    negate = -adxtail;
			    Two_Product(negate, cdytail, out var adxt_cdyt1, out var adxt_cdyt0);
			    Two_One_Product(adxt_cdyt1, adxt_cdyt0, bdz, out u3, out u[2], out u[1], out u[0]);
			    u[3] = u3;
			    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				    finother);
			    finswap = finnow; finnow = finother; finother = finswap;
			    if (bdztail != 0f) {
				    Two_One_Product(adxt_cdyt1, adxt_cdyt0, bdztail, out u3, out u[2], out u[1], out u[0]);
				    u[3] = u3;
				    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					    finother);
				    finswap = finnow; finnow = finother; finother = finswap;
			    }
		    }
	    }
	    if (bdxtail != 0f) {
		    if (cdytail != 0f) {
			    Two_Product(bdxtail, cdytail, out var bdxt_cdyt1, out var bdxt_cdyt0);
			    Two_One_Product(bdxt_cdyt1, bdxt_cdyt0, adz, out u3, out u[2], out u[1], out u[0]);
			    u[3] = u3;
			    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				    finother);
			    finswap = finnow; finnow = finother; finother = finswap;
			    if (adztail != 0f) {
				    Two_One_Product(bdxt_cdyt1, bdxt_cdyt0, adztail, out u3, out u[2], out u[1], out u[0]);
				    u[3] = u3;
				    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					    finother);
				    finswap = finnow; finnow = finother; finother = finswap;
			    }
		    }
		    if (adytail != 0f) {
			    negate = -bdxtail;
			    Two_Product(negate, adytail, out var bdxt_adyt1, out var bdxt_adyt0);
			    Two_One_Product(bdxt_adyt1, bdxt_adyt0, cdz, out u3, out u[2], out u[1], out u[0]);
			    u[3] = u3;
			    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				    finother);
			    finswap = finnow; finnow = finother; finother = finswap;
			    if (cdztail != 0f) {
				    Two_One_Product(bdxt_adyt1, bdxt_adyt0, cdztail, out u3, out u[2], out u[1], out u[0]);
				    u[3] = u3;
				    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					    finother);
				    finswap = finnow; finnow = finother; finother = finswap;
			    }
		    }
	    }
	    if (cdxtail != 0f) {
		    if (adytail != 0f) {
			    Two_Product(cdxtail, adytail, out var cdxt_adyt1, out var cdxt_adyt0);
			    Two_One_Product(cdxt_adyt1, cdxt_adyt0, bdz, out u3, out u[2], out u[1], out u[0]);
			    u[3] = u3;
			    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				    finother);
			    finswap = finnow; finnow = finother; finother = finswap;
			    if (bdztail != 0f) {
				    Two_One_Product(cdxt_adyt1, cdxt_adyt0, bdztail, out u3, out u[2], out u[1], out u[0]);
				    u[3] = u3;
				    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					    finother);
				    finswap = finnow; finnow = finother; finother = finswap;
			    }
		    }
		    if (bdytail != 0f) {
			    negate = -cdxtail;
			    Two_Product(negate, bdytail, out var cdxt_bdyt1, out var cdxt_bdyt0);
			    Two_One_Product(cdxt_bdyt1, cdxt_bdyt0, adz, out u3, out u[2], out u[1], out u[0]);
			    u[3] = u3;
			    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				    finother);
			    finswap = finnow; finnow = finother; finother = finswap;
			    if (adztail != 0f) {
				    Two_One_Product(cdxt_bdyt1, cdxt_bdyt0, adztail, out u3, out u[2], out u[1], out u[0]);
				    u[3] = u3;
				    finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					    finother);
				    finswap = finnow; finnow = finother; finother = finswap;
			    }
		    }
	    }

	    if (adztail != 0f) {
		    wlength = scale_expansion_zeroelim(bctlen, bct, adztail, w);
		    finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
			    finother);
		    finswap = finnow; finnow = finother; finother = finswap;
	    }
	    if (bdztail != 0f) {
		    wlength = scale_expansion_zeroelim(catlen, cat, bdztail, w);
		    finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
			    finother);
		    finswap = finnow; finnow = finother; finother = finswap;
	    }
	    if (cdztail != 0f) {
		    wlength = scale_expansion_zeroelim(abtlen, abt, cdztail, w);
		    finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
			    finother);
		    finswap = finnow; finnow = finother; finother = finswap;
	    }

	    return finnow[finlength - 1];
    }

    private void Two_One_Product(float a1, float a0, float b, out float x3, out float x2, out float x1, out float x0)
    {
        Split(b, out var bhi, out var blo);
        Two_Product_Presplit(a0, b, bhi, blo, out var i, out x0);
        Two_Product_Presplit(a1, b, bhi, blo, out var j, out var _0);
        Two_Sum(i, _0, out var k, out x1);
        Fast_Two_Sum(j, k, out x3, out x2);
    }

    public float InSphere(ref Vector3 pa, ref Vector3 pb, ref Vector3 pc, ref Vector3 pd, ref Vector3 pe)
    {
        float aex = pa[0] - pe[0];
        float bex = pb[0] - pe[0];
        float cex = pc[0] - pe[0];
        float dex = pd[0] - pe[0];
        float aey = pa[1] - pe[1];
        float bey = pb[1] - pe[1];
        float cey = pc[1] - pe[1];
        float dey = pd[1] - pe[1];
        float aez = pa[2] - pe[2];
        float bez = pb[2] - pe[2];
        float cez = pc[2] - pe[2];
        float dez = pd[2] - pe[2];

        float aexbey = aex * bey;
        float bexaey = bex * aey;
        float ab = aexbey - bexaey;
        float bexcey = bex * cey;
        float cexbey = cex * bey;
        float bc = bexcey - cexbey;
        float cexdey = cex * dey;
        float dexcey = dex * cey;
        float cd = cexdey - dexcey;
        float dexaey = dex * aey;
        float aexdey = aex * dey;
        float da = dexaey - aexdey;

        float aexcey = aex * cey;
        float cexaey = cex * aey;
        float ac = aexcey - cexaey;
        float bexdey = bex * dey;
        float dexbey = dex * bey;
        float bd = bexdey - dexbey;

        float abc = aez * bc - bez * ac + cez * ab;
        float bcd = bez * cd - cez * bd + dez * bc;
        float cda = cez * da + dez * ac + aez * cd;
        float dab = dez * ab + aez * bd + bez * da;

        float alift = aex * aex + aey * aey + aez * aez;
        float blift = bex * bex + bey * bey + bez * bez;
        float clift = cex * cex + cey * cey + cez * cez;
        float dlift = dex * dex + dey * dey + dez * dez;

        float det = (dlift * abc - clift * dab) + (blift * cda - alift * bcd);

        float aezplus = Math.Abs(aez);
        float bezplus = Math.Abs(bez);
        float cezplus = Math.Abs(cez);
        float dezplus = Math.Abs(dez);
        float aexbeyplus = Math.Abs(aexbey);
        float bexaeyplus = Math.Abs(bexaey);
        float bexceyplus = Math.Abs(bexcey);
        float cexbeyplus = Math.Abs(cexbey);
        float cexdeyplus = Math.Abs(cexdey);
        float dexceyplus = Math.Abs(dexcey);
        float dexaeyplus = Math.Abs(dexaey);
        float aexdeyplus = Math.Abs(aexdey);
        float aexceyplus = Math.Abs(aexcey);
        float cexaeyplus = Math.Abs(cexaey);
        float bexdeyplus = Math.Abs(bexdey);
        float dexbeyplus = Math.Abs(dexbey);
        float permanent = ((cexdeyplus + dexceyplus) * bezplus
                           + (dexbeyplus + bexdeyplus) * cezplus
                           + (bexceyplus + cexbeyplus) * dezplus)
                          * alift
                          + ((dexaeyplus + aexdeyplus) * cezplus
                             + (aexceyplus + cexaeyplus) * dezplus
                             + (cexdeyplus + dexceyplus) * aezplus)
                          * blift
                          + ((aexbeyplus + bexaeyplus) * dezplus
                             + (bexdeyplus + dexbeyplus) * aezplus
                             + (dexaeyplus + aexdeyplus) * bezplus)
                          * clift
                          + ((bexceyplus + cexbeyplus) * aezplus
                             + (cexaeyplus + aexceyplus) * bezplus
                             + (aexbeyplus + bexaeyplus) * cezplus)
                          * dlift;
        float errbound = isperrboundA * permanent;
        if ((det > errbound) || (-det > errbound)) {
            return det;
        }

        return InSphereAdapt(pa, pb, pc, pd, pe, permanent);
    }

    private float InSphereAdapt(Vector3 pa, Vector3 pb, Vector3 pc, Vector3 pd, Vector3 pe, float permanent)
    {
        Span<float> ab = stackalloc float[4];
        Span<float> bc = stackalloc float[4];
        Span<float> cd = stackalloc float[4];
        Span<float> da = stackalloc float[4];
        Span<float> ac = stackalloc float[4];
        Span<float> bd = stackalloc float[4];

        Span<float> temp8a = stackalloc float[8];
        Span<float> temp8b = stackalloc float[8];
        Span<float> temp8c = stackalloc float[8];
        Span<float> temp16 = stackalloc float[16];
        Span<float> temp24 = stackalloc float[24];
        Span<float> temp48 = stackalloc float[48];

        Span<float> xdet = stackalloc float[96];
        Span<float> ydet = stackalloc float[96];
        Span<float> zdet = stackalloc float[96];
        Span<float> xydet = stackalloc float[192];
        Span<float> adet = stackalloc float[288];
        Span<float> bdet = stackalloc float[288];
        Span<float> cdet = stackalloc float[288];
        Span<float> ddet = stackalloc float[288];
        Span<float> abdet = stackalloc float[576];
        Span<float> cddet = stackalloc float[576];
        Span<float> fin1 = stackalloc float[1152];

        float aex = pa[0] - pe[0];
	    float bex = pb[0] - pe[0];
	    float cex = pc[0] - pe[0];
	    float dex = pd[0] - pe[0];
	    float aey = pa[1] - pe[1];
	    float bey = pb[1] - pe[1];
	    float cey = pc[1] - pe[1];
	    float dey = pd[1] - pe[1];
	    float aez = pa[2] - pe[2];
	    float bez = pb[2] - pe[2];
	    float cez = pc[2] - pe[2];
	    float dez = pd[2] - pe[2];

	    Two_Product(aex, bey, out var aexbey1, out var aexbey0);
	    Two_Product(bex, aey, out var bexaey1, out var bexaey0);
	    Two_Two_Diff(aexbey1, aexbey0, bexaey1, bexaey0, out var ab3, out ab[2], out ab[1], out ab[0]);
	    ab[3] = ab3;

	    Two_Product(bex, cey, out var bexcey1, out var bexcey0);
	    Two_Product(cex, bey, out var cexbey1, out var cexbey0);
	    Two_Two_Diff(bexcey1, bexcey0, cexbey1, cexbey0, out var bc3, out bc[2], out bc[1], out bc[0]);
	    bc[3] = bc3;

	    Two_Product(cex, dey, out var cexdey1, out var cexdey0);
	    Two_Product(dex, cey, out var dexcey1, out var dexcey0);
	    Two_Two_Diff(cexdey1, cexdey0, dexcey1, dexcey0, out var cd3, out cd[2], out cd[1], out cd[0]);
	    cd[3] = cd3;

	    Two_Product(dex, aey, out var dexaey1, out var dexaey0);
	    Two_Product(aex, dey, out var aexdey1, out var aexdey0);
	    Two_Two_Diff(dexaey1, dexaey0, aexdey1, aexdey0, out var da3, out da[2], out da[1], out da[0]);
	    da[3] = da3;

	    Two_Product(aex, cey, out var aexcey1, out var aexcey0);
	    Two_Product(cex, aey, out var cexaey1, out var cexaey0);
	    Two_Two_Diff(aexcey1, aexcey0, cexaey1, cexaey0, out var ac3, out ac[2], out ac[1], out ac[0]);
	    ac[3] = ac3;

	    Two_Product(bex, dey, out var bexdey1, out var bexdey0);
	    Two_Product(dex, bey, out var dexbey1, out var dexbey0);
	    Two_Two_Diff(bexdey1, bexdey0, dexbey1, dexbey0, out var bd3, out bd[2], out bd[1], out bd[0]);
	    bd[3] = bd3;

	    int temp8alen = scale_expansion_zeroelim(4, cd, bez, temp8a);
	    int temp8blen = scale_expansion_zeroelim(4, bd, -cez, temp8b);
	    int temp8clen = scale_expansion_zeroelim(4, bc, dez, temp8c);
	    int temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a,
            temp8blen, temp8b, temp16);
	    int temp24len = fast_expansion_sum_zeroelim(temp8clen, temp8c,
            temp16len, temp16, temp24);
	    int temp48len = scale_expansion_zeroelim(temp24len, temp24, aex, temp48);
	    int xlen = scale_expansion_zeroelim(temp48len, temp48, -aex, xdet);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, aey, temp48);
	    int ylen = scale_expansion_zeroelim(temp48len, temp48, -aey, ydet);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, aez, temp48);
	    int zlen = scale_expansion_zeroelim(temp48len, temp48, -aez, zdet);
	    int xylen = fast_expansion_sum_zeroelim(xlen, xdet, ylen, ydet, xydet); 
	    int alen = fast_expansion_sum_zeroelim(xylen, xydet, zlen, zdet, adet);

	    temp8alen = scale_expansion_zeroelim(4, da, cez, temp8a);
	    temp8blen = scale_expansion_zeroelim(4, ac, dez, temp8b);
	    temp8clen = scale_expansion_zeroelim(4, cd, aez, temp8c);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a,
		    temp8blen, temp8b, temp16);
	    temp24len = fast_expansion_sum_zeroelim(temp8clen, temp8c,
		    temp16len, temp16, temp24);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, bex, temp48);
	    xlen = scale_expansion_zeroelim(temp48len, temp48, bex, xdet);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, bey, temp48);
	    ylen = scale_expansion_zeroelim(temp48len, temp48, bey, ydet);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, bez, temp48);
	    zlen = scale_expansion_zeroelim(temp48len, temp48, bez, zdet);
	    xylen = fast_expansion_sum_zeroelim(xlen, xdet, ylen, ydet, xydet);
	    int blen = fast_expansion_sum_zeroelim(xylen, xydet, zlen, zdet, bdet);

	    temp8alen = scale_expansion_zeroelim(4, ab, dez, temp8a);
	    temp8blen = scale_expansion_zeroelim(4, bd, aez, temp8b);
	    temp8clen = scale_expansion_zeroelim(4, da, bez, temp8c);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a,
		    temp8blen, temp8b, temp16);
	    temp24len = fast_expansion_sum_zeroelim(temp8clen, temp8c,
		    temp16len, temp16, temp24);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, cex, temp48);
	    xlen = scale_expansion_zeroelim(temp48len, temp48, -cex, xdet);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, cey, temp48);
	    ylen = scale_expansion_zeroelim(temp48len, temp48, -cey, ydet);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, cez, temp48);
	    zlen = scale_expansion_zeroelim(temp48len, temp48, -cez, zdet);
	    xylen = fast_expansion_sum_zeroelim(xlen, xdet, ylen, ydet, xydet);
	    int clen = fast_expansion_sum_zeroelim(xylen, xydet, zlen, zdet, cdet);

	    temp8alen = scale_expansion_zeroelim(4, bc, aez, temp8a);
	    temp8blen = scale_expansion_zeroelim(4, ac, -bez, temp8b);
	    temp8clen = scale_expansion_zeroelim(4, ab, cez, temp8c);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a,
		    temp8blen, temp8b, temp16);
	    temp24len = fast_expansion_sum_zeroelim(temp8clen, temp8c,
		    temp16len, temp16, temp24);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, dex, temp48);
	    xlen = scale_expansion_zeroelim(temp48len, temp48, dex, xdet);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, dey, temp48);
	    ylen = scale_expansion_zeroelim(temp48len, temp48, dey, ydet);
	    temp48len = scale_expansion_zeroelim(temp24len, temp24, dez, temp48);
	    zlen = scale_expansion_zeroelim(temp48len, temp48, dez, zdet);
	    xylen = fast_expansion_sum_zeroelim(xlen, xdet, ylen, ydet, xydet);
	    int dlen = fast_expansion_sum_zeroelim(xylen, xydet, zlen, zdet, ddet);

	    int ablen = fast_expansion_sum_zeroelim(alen, adet, blen, bdet, abdet);
	    int cdlen = fast_expansion_sum_zeroelim(clen, cdet, dlen, ddet, cddet);
	    int finlength = fast_expansion_sum_zeroelim(ablen, abdet, cdlen, cddet, fin1);

	    float det = estimate(finlength, fin1);
	    float errbound = isperrboundB * permanent;
	    if ((det >= errbound) || (-det >= errbound)) {
		    return det;
	    }

	    Two_Diff_Tail(pa[0], pe[0], aex, out var aextail);
	    Two_Diff_Tail(pa[1], pe[1], aey, out var aeytail);
	    Two_Diff_Tail(pa[2], pe[2], aez, out var aeztail);
	    Two_Diff_Tail(pb[0], pe[0], bex, out var bextail);
	    Two_Diff_Tail(pb[1], pe[1], bey, out var beytail);
	    Two_Diff_Tail(pb[2], pe[2], bez, out var beztail);
	    Two_Diff_Tail(pc[0], pe[0], cex, out var cextail);
	    Two_Diff_Tail(pc[1], pe[1], cey, out var ceytail);
	    Two_Diff_Tail(pc[2], pe[2], cez, out var ceztail);
	    Two_Diff_Tail(pd[0], pe[0], dex, out var dextail);
	    Two_Diff_Tail(pd[1], pe[1], dey, out var deytail);
	    Two_Diff_Tail(pd[2], pe[2], dez, out var deztail);
	    if ((aextail == 0f) && (aeytail == 0f) && (aeztail == 0f)
		    && (bextail == 0f) && (beytail == 0f) && (beztail == 0f)
		    && (cextail == 0f) && (ceytail == 0f) && (ceztail == 0f)
		    && (dextail == 0f) && (deytail == 0f) && (deztail == 0f)) {
		    return det;
	    }

	    errbound = isperrboundC * permanent + resulterrbound * Math.Abs(det);
	    float abeps = (aex * beytail + bey * aextail)
                      - (aey * bextail + bex * aeytail);
	    float bceps = (bex * ceytail + cey * bextail)
                      - (bey * cextail + cex * beytail);
	    float cdeps = (cex * deytail + dey * cextail)
                      - (cey * dextail + dex * ceytail);
	    float daeps = (dex * aeytail + aey * dextail)
                      - (dey * aextail + aex * deytail);
	    float aceps = (aex * ceytail + cey * aextail)
                      - (aey * cextail + cex * aeytail);
	    float bdeps = (bex * deytail + dey * bextail)
                      - (bey * dextail + dex * beytail);
	    det += (((bex * bex + bey * bey + bez * bez)
		    * ((cez * daeps + dez * aceps + aez * cdeps)
		    + (ceztail * da3 + deztail * ac3 + aeztail * cd3))
		    + (dex * dex + dey * dey + dez * dez)
		    * ((aez * bceps - bez * aceps + cez * abeps)
		    + (aeztail * bc3 - beztail * ac3 + ceztail * ab3)))
		    - ((aex * aex + aey * aey + aez * aez)
		    * ((bez * cdeps - cez * bdeps + dez * bceps)
		    + (beztail * cd3 - ceztail * bd3 + deztail * bc3))
		    + (cex * cex + cey * cey + cez * cez)
		    * ((dez * abeps + aez * bdeps + bez * daeps)
		    + (deztail * ab3 + aeztail * bd3 + beztail * da3))))
		    + 2.0f * (((bex * bextail + bey * beytail + bez * beztail)
		    * (cez * da3 + dez * ac3 + aez * cd3)
		    + (dex * dextail + dey * deytail + dez * deztail)
		    * (aez * bc3 - bez * ac3 + cez * ab3))
		    - ((aex * aextail + aey * aeytail + aez * aeztail)
		    * (bez * cd3 - cez * bd3 + dez * bc3)
		    + (cex * cextail + cey * ceytail + cez * ceztail)
		    * (dez * ab3 + aez * bd3 + bez * da3)));
	    if ((det >= errbound) || (-det >= errbound)) {
		    return det;
	    }

	    return InSphereExact(pa, pb, pc, pd, pe);
    }

    private static float estimate(int elen, Span<float> e)
    {
        int eindex;

        float Q = e[0];
        for (eindex = 1; eindex < elen; eindex++) {
            Q += e[eindex];
        }
        return Q;
    }

    private static void Two_Diff_Tail(float a, float b, float x, out float y)
    {
        var bvirt = a - x;
        var avirt = x + bvirt;
        var bround = bvirt - b;
        var around = a - avirt;
        y = around + bround;
    }

    private static int fast_expansion_sum_zeroelim(int elen, Span<float> e, int flen, Span<float> f, Span<float> h)
    {
        float Q;
        float Qnew;
        float hh;
        int findex;

        float enow = e[0];
        float fnow = f[0];
        int eindex = findex = 0;
        if ((fnow > enow) == (fnow > -enow)) {
            Q = enow;
            enow = e[++eindex];
        }
        else {
            Q = fnow;
            fnow = f[++findex];
        }
        int hindex = 0;
        if ((eindex < elen) && (findex < flen)) {
            if ((fnow > enow) == (fnow > -enow)) {
                Fast_Two_Sum(enow, Q, out Qnew, out hh);
                enow = e[++eindex];
            }
            else {
                Fast_Two_Sum(fnow, Q, out Qnew, out hh);
                fnow = f[++findex];
            }
            Q = Qnew;
            if (hh != 0f) {
                h[hindex++] = hh;
            }
            while ((eindex < elen) && (findex < flen)) {
                if ((fnow > enow) == (fnow > -enow)) {
                    Two_Sum(Q, enow, out Qnew, out hh);
                    enow = e[++eindex];
                }
                else {
                    Two_Sum(Q, fnow, out Qnew, out hh);
                    fnow = f[++findex];
                }
                Q = Qnew;
                if (hh != 0f) {
                    h[hindex++] = hh;
                }
            }
        }
        while (eindex < elen) {
            Two_Sum(Q, enow, out Qnew, out hh);
            enow = e[++eindex];
            Q = Qnew;
            if (hh != 0f) {
                h[hindex++] = hh;
            }
        }
        while (findex < flen) {
            Two_Sum(Q, fnow, out Qnew, out hh);
            fnow = f[++findex];
            Q = Qnew;
            if (hh != 0f) {
                h[hindex++] = hh;
            }
        }
        if ((Q != 0f) || (hindex == 0f)) {
            h[hindex++] = Q;
        }
        return hindex;
    }

    private static void Fast_Two_Sum(float a, float b, out float x, out float y)
    {
        x = a + b;
        Fast_Two_Sum_Tail(a, b, x, out y);
    }

    private static void Fast_Two_Sum_Tail(float a, float b, float x, out float y)
    {
        float bvirt = x - a;
        y = b - bvirt;
    }

    private int scale_expansion_zeroelim(int elen, Span<float> e, float b, Span<float> h)
    {
        float sum;
        float product1;
        float product0;
        int eindex;
        float enow;

        Split(b, out var bhi, out var blo);
        Two_Product_Presplit(e[0], b, bhi, blo, out var Q, out var hh);
        int hindex = 0;
        if (hh != 0f) {
            h[hindex++] = hh;
        }
        for (eindex = 1; eindex < elen; eindex++) {
            enow = e[eindex];
            Two_Product_Presplit(enow, b, bhi, blo, out product1, out product0);
            Two_Sum(Q, product0, out sum, out hh);
            if (hh != 0f) {
                h[hindex++] = hh;
            }
            Fast_Two_Sum(product1, sum, out Q, out hh);
            if (hh != 0f) {
                h[hindex++] = hh;
            }
        }
        if ((Q != 0f) || (hindex == 0f)) {
            h[hindex++] = Q;
        }
        return hindex;
    }

    private void Two_Product_Presplit(float a, float b, float bhi, float blo, out float x, out float y)
    {
        x = a * b;
        Split(a, out var ahi, out var alo);
        float err1 = x - (ahi * bhi);
        float err2 = err1 - (alo * bhi);
        float err3 = err2 - (ahi * blo);
        y = (alo * blo) - err3;
    }

    private static void Two_Two_Diff(float a1, float a0, float b1, float b0, out float x3, out float x2, out float x1, out float x0)
    {
        Two_One_Diff(a1, a0, b0, out var j, out var _0, out x0);
        Two_One_Diff(j, _0, b1, out x3, out x2, out x1);
    }

    private static void Two_One_Diff(float a1, float a0, float b, out float x2, out float x1, out float x0)
    {
        Two_Diff(a0, b , out var i, out x0);
        Two_Sum(a1, i, out x2, out x1);
    }

    private static void Two_Sum(float a, float b, out float x, out float y)
    {
        x = a + b;
        Two_Sum_Tail(a, b, x, out y);
    }

    private static void Two_Sum_Tail(float a, float b, float x, out float y)
    {
        float bvirt = (x - a);
        float avirt = x - bvirt; 
        float bround = b - bvirt; 
        float around = a - avirt;
        y = around + bround;
    }

    private static void Two_Diff(float a, float b, out float x, out float y)
    {
        x = a - b;
        Two_Diff_Tail(a, b, x, out y);
    }

    private void Two_Product(float a, float b, out float x, out float y)
    {
        x = a * b;
        Two_Product_Tail(a, b, x, out y);
    }

    private void Two_Product_Tail(float a, float b, float x, out float y)
    {
        Split(a, out var ahi, out var alo); 
        Split(b, out var bhi, out var blo); 
        float err1 = x - (ahi * bhi); 
        float err2 = err1 - (alo * bhi); 
        float err3 = err2 - (ahi * blo);
        y = (alo * blo) - err3;
    }

    private void Split(float a, out float ahi, out float alo)
    {
        float c = (splitter * a);
        float abig = c - a; 
        ahi = c - abig;
        alo = a - ahi;
    }

    private float InSphereExact(Vector3 pa, Vector3 pb, Vector3 pc, Vector3 pd, Vector3 pe)
    {
        Span<float> ab = stackalloc float[4];
        Span<float> bc = stackalloc float[4];
        Span<float> cd = stackalloc float[4];
        Span<float> de = stackalloc float[4];
        Span<float> ea = stackalloc float[4];
        Span<float> ac = stackalloc float[4];
        Span<float> bd = stackalloc float[4];
        Span<float> ce = stackalloc float[4];
        Span<float> da = stackalloc float[4];
        Span<float> eb = stackalloc float[4];
        
        Span<float> temp8a = stackalloc float[8];
        Span<float> temp8b = stackalloc float[8];
        Span<float> temp16 = stackalloc float[16];

        Span<float> abc = stackalloc float[24];
        Span<float> bcd = stackalloc float[24];
        Span<float> cde = stackalloc float[24];
        Span<float> dea = stackalloc float[24];
        Span<float> eab = stackalloc float[24];
        Span<float> abd = stackalloc float[24];
        Span<float> bce = stackalloc float[24];
        Span<float> cda = stackalloc float[24];
        Span<float> deb = stackalloc float[24];
        Span<float> eac = stackalloc float[24];

        Span<float> temp48a = stackalloc float[48];
        Span<float> temp48b = stackalloc float[48];
        
        Span<float> abcd = stackalloc float[96];
        Span<float> bcde = stackalloc float[96];
        Span<float> cdea = stackalloc float[96];
        Span<float> deab = stackalloc float[96];
        Span<float> eabc = stackalloc float[96];
        
        Span<float> temp192 = stackalloc float[192];
        Span<float> det384x = stackalloc float[384];
        Span<float> det384y = stackalloc float[384];
        Span<float> det384z = stackalloc float[384];
        Span<float> detxy = stackalloc float[768];
        Span<float> adet = stackalloc float[1152];
        Span<float> bdet = stackalloc float[1152];
        Span<float> cdet = stackalloc float[1152];
        Span<float> ddet = stackalloc float[1152];
        Span<float> edet = stackalloc float[1152];
        Span<float> abdet = stackalloc float[2304];
        Span<float> cddet = stackalloc float[2304];
        Span<float> cdedet = stackalloc float[3456];
        Span<float> deter = stackalloc float[5760];
        int i;

	    Two_Product(pa[0], pb[1], out var axby1, out var axby0);
	    Two_Product(pb[0], pa[1], out var bxay1, out var bxay0);
	    Two_Two_Diff(axby1, axby0, bxay1, bxay0, out ab[3], out ab[2], out ab[1], out ab[0]);

	    Two_Product(pb[0], pc[1], out var bxcy1, out var bxcy0);
	    Two_Product(pc[0], pb[1], out var cxby1, out var cxby0);
	    Two_Two_Diff(bxcy1, bxcy0, cxby1, cxby0, out bc[3], out bc[2], out bc[1], out bc[0]);

	    Two_Product(pc[0], pd[1], out var cxdy1, out var cxdy0);
	    Two_Product(pd[0], pc[1], out var dxcy1, out var dxcy0);
	    Two_Two_Diff(cxdy1, cxdy0, dxcy1, dxcy0, out cd[3], out cd[2], out cd[1], out cd[0]);

	    Two_Product(pd[0], pe[1], out var dxey1, out var dxey0);
	    Two_Product(pe[0], pd[1], out var exdy1, out var exdy0);
	    Two_Two_Diff(dxey1, dxey0, exdy1, exdy0, out de[3], out de[2], out de[1], out de[0]);

	    Two_Product(pe[0], pa[1], out var exay1, out var exay0);
	    Two_Product(pa[0], pe[1], out var axey1, out var axey0);
	    Two_Two_Diff(exay1, exay0, axey1, axey0, out ea[3], out ea[2], out ea[1], out ea[0]);

	    Two_Product(pa[0], pc[1], out var axcy1, out var axcy0);
	    Two_Product(pc[0], pa[1], out var cxay1, out var cxay0);
	    Two_Two_Diff(axcy1, axcy0, cxay1, cxay0, out ac[3], out ac[2], out ac[1], out ac[0]);

	    Two_Product(pb[0], pd[1], out var bxdy1, out var bxdy0);
	    Two_Product(pd[0], pb[1], out var dxby1, out var dxby0);
	    Two_Two_Diff(bxdy1, bxdy0, dxby1, dxby0, out bd[3], out bd[2], out bd[1], out bd[0]);

	    Two_Product(pc[0], pe[1], out var cxey1, out var cxey0);
	    Two_Product(pe[0], pc[1], out var excy1, out var excy0);
	    Two_Two_Diff(cxey1, cxey0, excy1, excy0, out ce[3], out ce[2], out ce[1], out ce[0]);

	    Two_Product(pd[0], pa[1], out var dxay1, out var dxay0);
	    Two_Product(pa[0], pd[1], out var axdy1, out var axdy0);
	    Two_Two_Diff(dxay1, dxay0, axdy1, axdy0, out da[3], out da[2], out da[1], out da[0]);

	    Two_Product(pe[0], pb[1], out var exby1, out var exby0);
	    Two_Product(pb[0], pe[1], out var bxey1, out var bxey0);
	    Two_Two_Diff(exby1, exby0, bxey1, bxey0, out eb[3], out eb[2], out eb[1], out eb[0]);

	    int temp8alen = scale_expansion_zeroelim(4, bc, pa[2], temp8a);
	    int temp8blen = scale_expansion_zeroelim(4, ac, -pb[2], temp8b);
	    int temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
            temp16);
	    temp8alen = scale_expansion_zeroelim(4, ab, pc[2], temp8a);
	    int abclen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            abc);

	    temp8alen = scale_expansion_zeroelim(4, cd, pb[2], temp8a);
	    temp8blen = scale_expansion_zeroelim(4, bd, -pc[2], temp8b);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		    temp16);
	    temp8alen = scale_expansion_zeroelim(4, bc, pd[2], temp8a);
	    int bcdlen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            bcd);

	    temp8alen = scale_expansion_zeroelim(4, de, pc[2], temp8a);
	    temp8blen = scale_expansion_zeroelim(4, ce, -pd[2], temp8b);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		    temp16);
	    temp8alen = scale_expansion_zeroelim(4, cd, pe[2], temp8a);
	    var cdelen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            cde);

	    temp8alen = scale_expansion_zeroelim(4, ea, pd[2], temp8a);
	    temp8blen = scale_expansion_zeroelim(4, da, -pe[2], temp8b);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		    temp16);
	    temp8alen = scale_expansion_zeroelim(4, de, pa[2], temp8a);
	    var dealen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            dea);

	    temp8alen = scale_expansion_zeroelim(4, ab, pe[2], temp8a);
	    temp8blen = scale_expansion_zeroelim(4, eb, -pa[2], temp8b);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		    temp16);
	    temp8alen = scale_expansion_zeroelim(4, ea, pb[2], temp8a);
	    var eablen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            eab);

	    temp8alen = scale_expansion_zeroelim(4, bd, pa[2], temp8a);
	    temp8blen = scale_expansion_zeroelim(4, da, pb[2], temp8b);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		    temp16);
	    temp8alen = scale_expansion_zeroelim(4, ab, pd[2], temp8a);
	    var abdlen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            abd);

	    temp8alen = scale_expansion_zeroelim(4, ce, pb[2], temp8a);
	    temp8blen = scale_expansion_zeroelim(4, eb, pc[2], temp8b);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		    temp16);
	    temp8alen = scale_expansion_zeroelim(4, bc, pe[2], temp8a);
	    var bcelen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            bce);

	    temp8alen = scale_expansion_zeroelim(4, da, pc[2], temp8a);
	    temp8blen = scale_expansion_zeroelim(4, ac, pd[2], temp8b);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		    temp16);
	    temp8alen = scale_expansion_zeroelim(4, cd, pa[2], temp8a);
	    var cdalen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            cda);

	    temp8alen = scale_expansion_zeroelim(4, eb, pd[2], temp8a);
	    temp8blen = scale_expansion_zeroelim(4, bd, pe[2], temp8b);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		    temp16);
	    temp8alen = scale_expansion_zeroelim(4, de, pb[2], temp8a);
	    var deblen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            deb);

	    temp8alen = scale_expansion_zeroelim(4, ac, pe[2], temp8a);
	    temp8blen = scale_expansion_zeroelim(4, ce, pa[2], temp8b);
	    temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		    temp16);
	    temp8alen = scale_expansion_zeroelim(4, ea, pc[2], temp8a);
	    int eaclen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
            eac);

	    int temp48alen = fast_expansion_sum_zeroelim(cdelen, cde, bcelen, bce, temp48a);
	    int temp48blen = fast_expansion_sum_zeroelim(deblen, deb, bcdlen, bcd, temp48b);
	    for (i = 0; i < temp48blen; i++) {
		    temp48b[i] = -temp48b[i];
	    }
	    int bcdelen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
            temp48blen, temp48b, bcde);
	    int xlen = scale_expansion_zeroelim(bcdelen, bcde, pa[0], temp192);
	    xlen = scale_expansion_zeroelim(xlen, temp192, pa[0], det384x);
	    int ylen = scale_expansion_zeroelim(bcdelen, bcde, pa[1], temp192);
	    ylen = scale_expansion_zeroelim(ylen, temp192, pa[1], det384y);
	    int zlen = scale_expansion_zeroelim(bcdelen, bcde, pa[2], temp192);
	    zlen = scale_expansion_zeroelim(zlen, temp192, pa[2], det384z);
	    int xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	    int alen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, adet);

	    temp48alen = fast_expansion_sum_zeroelim(dealen, dea, cdalen, cda, temp48a);
	    temp48blen = fast_expansion_sum_zeroelim(eaclen, eac, cdelen, cde, temp48b);
	    for (i = 0; i < temp48blen; i++) {
		    temp48b[i] = -temp48b[i];
	    }
	    var cdealen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
            temp48blen, temp48b, cdea);
	    xlen = scale_expansion_zeroelim(cdealen, cdea, pb[0], temp192);
	    xlen = scale_expansion_zeroelim(xlen, temp192, pb[0], det384x);
	    ylen = scale_expansion_zeroelim(cdealen, cdea, pb[1], temp192);
	    ylen = scale_expansion_zeroelim(ylen, temp192, pb[1], det384y);
	    zlen = scale_expansion_zeroelim(cdealen, cdea, pb[2], temp192);
	    zlen = scale_expansion_zeroelim(zlen, temp192, pb[2], det384z);
	    xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	    int blen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, bdet);

	    temp48alen = fast_expansion_sum_zeroelim(eablen, eab, deblen, deb, temp48a);
	    temp48blen = fast_expansion_sum_zeroelim(abdlen, abd, dealen, dea, temp48b);
	    for (i = 0; i < temp48blen; i++) {
		    temp48b[i] = -temp48b[i];
	    }
	    int deablen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
            temp48blen, temp48b, deab);
	    xlen = scale_expansion_zeroelim(deablen, deab, pc[0], temp192);
	    xlen = scale_expansion_zeroelim(xlen, temp192, pc[0], det384x);
	    ylen = scale_expansion_zeroelim(deablen, deab, pc[1], temp192);
	    ylen = scale_expansion_zeroelim(ylen, temp192, pc[1], det384y);
	    zlen = scale_expansion_zeroelim(deablen, deab, pc[2], temp192);
	    zlen = scale_expansion_zeroelim(zlen, temp192, pc[2], det384z);
	    xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	    int clen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, cdet);

	    temp48alen = fast_expansion_sum_zeroelim(abclen, abc, eaclen, eac, temp48a);
	    temp48blen = fast_expansion_sum_zeroelim(bcelen, bce, eablen, eab, temp48b);
	    for (i = 0; i < temp48blen; i++) {
		    temp48b[i] = -temp48b[i];
	    }
	    int eabclen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
            temp48blen, temp48b, eabc);
	    xlen = scale_expansion_zeroelim(eabclen, eabc, pd[0], temp192);
	    xlen = scale_expansion_zeroelim(xlen, temp192, pd[0], det384x);
	    ylen = scale_expansion_zeroelim(eabclen, eabc, pd[1], temp192);
	    ylen = scale_expansion_zeroelim(ylen, temp192, pd[1], det384y);
	    zlen = scale_expansion_zeroelim(eabclen, eabc, pd[2], temp192);
	    zlen = scale_expansion_zeroelim(zlen, temp192, pd[2], det384z);
	    xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	    int dlen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, ddet);

	    temp48alen = fast_expansion_sum_zeroelim(bcdlen, bcd, abdlen, abd, temp48a);
	    temp48blen = fast_expansion_sum_zeroelim(cdalen, cda, abclen, abc, temp48b);
	    for (i = 0; i < temp48blen; i++) {
		    temp48b[i] = -temp48b[i];
	    }
	    int abcdlen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
            temp48blen, temp48b, abcd);
	    xlen = scale_expansion_zeroelim(abcdlen, abcd, pe[0], temp192);
	    xlen = scale_expansion_zeroelim(xlen, temp192, pe[0], det384x);
	    ylen = scale_expansion_zeroelim(abcdlen, abcd, pe[1], temp192);
	    ylen = scale_expansion_zeroelim(ylen, temp192, pe[1], det384y);
	    zlen = scale_expansion_zeroelim(abcdlen, abcd, pe[2], temp192);
	    zlen = scale_expansion_zeroelim(zlen, temp192, pe[2], det384z);
	    xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	    int elen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, edet);

	    int ablen = fast_expansion_sum_zeroelim(alen, adet, blen, bdet, abdet);
	    int cdlen = fast_expansion_sum_zeroelim(clen, cdet, dlen, ddet, cddet);
	    cdelen = fast_expansion_sum_zeroelim(cdlen, cddet, elen, edet, cdedet);
	    int deterlen = fast_expansion_sum_zeroelim(ablen, abdet, cdelen, cdedet, deter);

	    return deter[deterlen - 1];
    }

}
