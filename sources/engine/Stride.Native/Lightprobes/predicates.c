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

/*****************************************************************************/
/*                                                                           */
/*  Using this code:                                                         */
/*                                                                           */
/*  First, read the short or long version of the paper (from the Web page    */
/*    above).                                                                */
/*                                                                           */
/*  Be sure to call exactinit() once, before calling any of the arithmetic   */
/*    functions or geometric predicates.  Also be sure to turn on the        */
/*    optimizer when compiling this file.                                    */
/*                                                                           */
/*                                                                           */
/*  Several geometric predicates are defined.  Their parameters are all      */
/*    points.  Each point is an array of two or three floating-point         */
/*    numbers.  The geometric predicates, described in the papers, are       */
/*                                                                           */
/*    orient3d(pa, pb, pc, pd)                                               */
/*    incircle(pa, pb, pc, pd)                                               */
/*    insphere(pa, pb, pc, pd, pe)                                           */
/*                                                                           */
/*  Those with suffix "fast" are approximate, non-robust versions.  Those    */
/*    without the suffix are adaptive precision, robust versions.  There     */
/*    are also versions with the suffices "exact" and "slow", which are      */
/*    non-adaptive, exact arithmetic versions, which I use only for timings  */
/*    in my arithmetic papers.                                               */
/*                                                                           */
/*                                                                           */
/*  An expansion is represented by an array of floating-point numbers,       */
/*    sorted from smallest to largest magnitude (possibly with interspersed  */
/*    zeros).  The length of each expansion is stored as a separate integer, */
/*    and each arithmetic function returns an integer which is the length    */
/*    of the expansion it created.                                           */
/*                                                                           */
/*  Several arithmetic functions are defined.  Their parameters are          */
/*                                                                           */
/*    e, f           Input expansions                                        */
/*    elen, flen     Lengths of input expansions (must be >= 1)              */
/*    h              Output expansion                                        */
/*    b              Input scalar                                            */
/*                                                                           */
/*  The arithmetic functions are                                             */
/*                                                                           */
/*    fast_expansion_sum_zeroelim(elen, e, flen, f, h)                       */
/*    scale_expansion_zeroelim(elen, e, b, h)                                */
/*    compress(elen, e, h)                                                   */
/*                                                                           */
/*  All of these are described in the long version of the paper; some are    */
/*    described in the short version.  All return an integer that is the     */
/*    length of h.  Those with suffix _zeroelim perform zero elimination,    */
/*    and are recommended over their counterparts.  The procedure            */
/*    fast_expansion_sum_zeroelim() is                                       */
/*    recommended over expansion_sum_zeroelim().  Each procedure has a       */
/*    little note next to it (in the code below) that tells you whether or   */
/*    not the output expansion may be the same array as one of the input     */
/*    expansions.                                                            */
/*                                                                           */
/*                                                                           */
/*  If you look around below, you'll also find macros for a bunch of         */
/*    simple unrolled arithmetic operations, and procedures for printing     */
/*    expansions (commented out because they don't work with all C           */
/*    compilers) and for generating random floating-point numbers whose      */
/*    significand bits are all random.  Most of the macros have undocumented */
/*    requirements that certain of their parameters should not be the same   */
/*    variable; for safety, better to make sure all the parameters are       */
/*    distinct variables.  Feel free to send email to jrs@cs.cmu.edu if you  */
/*    have questions.                                                        */
/*                                                                           */
/*****************************************************************************/

#include "../StrideNative.h"
#include "../../../deps/NativePath/NativePath.h"
#include "../../../deps/NativePath/standard/stdlib.h"

/* On some machines, the exact arithmetic routines might be defeated by the  */
/*   use of internal extended precision floating-point registers.  Sometimes */
/*   this problem can be fixed by defining certain values to be volatile,    */
/*   thus forcing them to be stored to memory and rounded off.  This isn't   */
/*   a great solution, though, as it slows the arithmetic down.              */
/*                                                                           */
/* To try this out, write "#define INEXACT volatile" below.  Normally,       */
/*   however, INEXACT should be defined to be nothing.  ("#define INEXACT".) */

#define INEXACT                          /* Nothing */
/* #define INEXACT volatile */

#define REAL float                      /* float or double */

/* Which of the following two methods of finding the absolute values is      */
/*   fastest is compiler-dependent.  A few compilers can inline and optimize */
/*   the fabs() call; but most will incur the overhead of a function call,   */
/*   which is disastrously slow.  A faster way on IEEE machines might be to  */
/*   mask the appropriate bit, but that's difficult to do in C.              */

#define Absolute(a)  ((a) >= 0.0 ? (a) : -(a))
/* #define Absolute(a)  fabs(a) */

/* Many of the operations are broken up into two pieces, a main part that    */
/*   performs an approximate operation, and a "tail" that computes the       */
/*   roundoff error of that operation.                                       */
/*                                                                           */
/* The operations Fast_Two_Sum(), Two_Sum(), Two_Diff(),    */
/*   Split(), and Two_Product() are all implemented as described in the      */
/*   reference.  Each of these macros requires certain variables to be       */
/*   defined in the calling routine.  The variables `bvirt', `c', `abig',    */
/*   `_i', `_j', `_k', `_l', `_m', and `_n' are declared `INEXACT' because   */
/*   they store the result of an operation that may incur roundoff error.    */
/*   The input parameter `x' (or the highest numbered `x_' parameter) must   */
/*   also be declared `INEXACT'.                                             */

#define Fast_Two_Sum_Tail(a, b, x, y) \
  bvirt = x - a; \
  y = b - bvirt

#define Fast_Two_Sum(a, b, x, y) \
  x = (REAL) (a + b); \
  Fast_Two_Sum_Tail(a, b, x, y)

#define Fast_Two_Diff_Tail(a, b, x, y) \
  bvirt = a - x; \
  y = bvirt - b

#define Two_Sum_Tail(a, b, x, y) \
  bvirt = (REAL) (x - a); \
  avirt = x - bvirt; \
  bround = b - bvirt; \
  around = a - avirt; \
  y = around + bround

#define Two_Sum(a, b, x, y) \
  x = (REAL) (a + b); \
  Two_Sum_Tail(a, b, x, y)

#define Two_Diff_Tail(a, b, x, y) \
  bvirt = (REAL) (a - x); \
  avirt = x + bvirt; \
  bround = bvirt - b; \
  around = a - avirt; \
  y = around + bround

#define Two_Diff(a, b, x, y) \
  x = (REAL) (a - b); \
  Two_Diff_Tail(a, b, x, y)

#define Split(a, ahi, alo) \
  c = (REAL) (splitter * a); \
  abig = (REAL) (c - a); \
  ahi = c - abig; \
  alo = a - ahi

#define Two_Product_Tail(a, b, x, y) \
  Split(a, ahi, alo); \
  Split(b, bhi, blo); \
  err1 = x - (ahi * bhi); \
  err2 = err1 - (alo * bhi); \
  err3 = err2 - (ahi * blo); \
  y = (alo * blo) - err3

#define Two_Product(a, b, x, y) \
  x = (REAL) (a * b); \
  Two_Product_Tail(a, b, x, y)

/* Two_Product_Presplit() is Two_Product() where one of the inputs has       */
/*   already been split.  Avoids redundant splitting.                        */

#define Two_Product_Presplit(a, b, bhi, blo, x, y) \
  x = (REAL) (a * b); \
  Split(a, ahi, alo); \
  err1 = x - (ahi * bhi); \
  err2 = err1 - (alo * bhi); \
  err3 = err2 - (ahi * blo); \
  y = (alo * blo) - err3

/* Two_Product_2Presplit() is Two_Product() where both of the inputs have    */
/*   already been split.  Avoids redundant splitting.                        */

#define Two_Product_2Presplit(a, ahi, alo, b, bhi, blo, x, y) \
  x = (REAL) (a * b); \
  err1 = x - (ahi * bhi); \
  err2 = err1 - (alo * bhi); \
  err3 = err2 - (ahi * blo); \
  y = (alo * blo) - err3

/* Square() can be done more quickly than Two_Product().                     */

#define Square_Tail(a, x, y) \
  Split(a, ahi, alo); \
  err1 = x - (ahi * ahi); \
  err3 = err1 - ((ahi + ahi) * alo); \
  y = (alo * alo) - err3

#define Square(a, x, y) \
  x = (REAL) (a * a); \
  Square_Tail(a, x, y)

/* Macros for summing expansions of various fixed lengths.  These are all    */
/*   unrolled versions of Expansion_Sum().                                   */

#define Two_One_Sum(a1, a0, b, x2, x1, x0) \
  Two_Sum(a0, b , _i, x0); \
  Two_Sum(a1, _i, x2, x1)

#define Two_One_Diff(a1, a0, b, x2, x1, x0) \
  Two_Diff(a0, b , _i, x0); \
  Two_Sum( a1, _i, x2, x1)

#define Two_Two_Sum(a1, a0, b1, b0, x3, x2, x1, x0) \
  Two_One_Sum(a1, a0, b0, _j, _0, x0); \
  Two_One_Sum(_j, _0, b1, x3, x2, x1)

#define Two_Two_Diff(a1, a0, b1, b0, x3, x2, x1, x0) \
  Two_One_Diff(a1, a0, b0, _j, _0, x0); \
  Two_One_Diff(_j, _0, b1, x3, x2, x1)

/* Macros for multiplying expansions of various fixed lengths.               */

#define Two_One_Product(a1, a0, b, x3, x2, x1, x0) \
  Split(b, bhi, blo); \
  Two_Product_Presplit(a0, b, bhi, blo, _i, x0); \
  Two_Product_Presplit(a1, b, bhi, blo, _j, _0); \
  Two_Sum(_i, _0, _k, x1); \
  Fast_Two_Sum(_j, _k, x3, x2)

REAL splitter;     /* = 2^ceiling(p / 2) + 1.  Used to split floats in half. */
REAL epsilon;                /* = 2^(-p).  Used to estimate roundoff errors. */
/* A set of coefficients used to calculate maximum roundoff errors.          */
REAL resulterrbound;
REAL ccwerrboundA, ccwerrboundB, ccwerrboundC;
REAL o3derrboundA, o3derrboundB, o3derrboundC;
REAL iccerrboundA, iccerrboundB, iccerrboundC;
REAL isperrboundA, isperrboundB, isperrboundC;

/*****************************************************************************/
/*                                                                           */
/*  exactinit()   Initialize the variables used for exact arithmetic.        */
/*                                                                           */
/*  `epsilon' is the largest power of two such that 1.0 + epsilon = 1.0 in   */
/*  floating-point arithmetic.  `epsilon' bounds the relative roundoff       */
/*  error.  It is used for floating-point error analysis.                    */
/*                                                                           */
/*  `splitter' is used to split floating-point numbers into two half-        */
/*  length significands for exact multiplication.                            */
/*                                                                           */
/*  I imagine that a highly optimizing compiler might be too smart for its   */
/*  own good, and somehow cause this routine to fail, if it pretends that    */
/*  floating-point arithmetic is too much like real arithmetic.              */
/*                                                                           */
/*  Don't change this routine unless you fully understand it.                */
/*                                                                           */
/*****************************************************************************/

DLL_EXPORT_API void exactinit()
{
	REAL half;
	REAL check, lastcheck;
	int every_other;

	every_other = 1;
	half = 0.5;
	epsilon = 1.0;
	splitter = 1.0;
	check = 1.0;
	/* Repeatedly divide `epsilon' by two until it is too small to add to    */
	/*   one without causing roundoff.  (Also check if the sum is equal to   */
	/*   the previous sum, for machines that round up instead of using exact */
	/*   rounding.  Not that this library will work on such machines anyway. */
	do {
		lastcheck = check;
		epsilon *= half;
		if (every_other) {
			splitter *= 2.0;
		}
		every_other = !every_other;
		check = 1.0 + epsilon;
	} while ((check != 1.0) && (check != lastcheck));
	splitter += 1.0;

	/* Error bounds for orientation and incircle tests. */
	resulterrbound = (3.0 + 8.0 * epsilon) * epsilon;
	ccwerrboundA = (3.0 + 16.0 * epsilon) * epsilon;
	ccwerrboundB = (2.0 + 12.0 * epsilon) * epsilon;
	ccwerrboundC = (9.0 + 64.0 * epsilon) * epsilon * epsilon;
	o3derrboundA = (7.0 + 56.0 * epsilon) * epsilon;
	o3derrboundB = (3.0 + 28.0 * epsilon) * epsilon;
	o3derrboundC = (26.0 + 288.0 * epsilon) * epsilon * epsilon;
	iccerrboundA = (10.0 + 96.0 * epsilon) * epsilon;
	iccerrboundB = (4.0 + 48.0 * epsilon) * epsilon;
	iccerrboundC = (44.0 + 576.0 * epsilon) * epsilon * epsilon;
	isperrboundA = (16.0 + 224.0 * epsilon) * epsilon;
	isperrboundB = (5.0 + 72.0 * epsilon) * epsilon;
	isperrboundC = (71.0 + 1408.0 * epsilon) * epsilon * epsilon;
}

/*****************************************************************************/
/*                                                                           */
/*  fast_expansion_sum_zeroelim()   Sum two expansions, eliminating zero     */
/*                                  components from the output expansion.    */
/*                                                                           */
/*  Sets h = e + f.  See the long version of my paper for details.           */
/*                                                                           */
/*  If round-to-even is used (as with IEEE 754), maintains the strongly      */
/*  nonoverlapping property.  (That is, if e is strongly nonoverlapping, h   */
/*  will be also.)  Does NOT maintain the nonoverlapping or nonadjacent      */
/*  properties.                                                              */
/*                                                                           */
/*****************************************************************************/

int fast_expansion_sum_zeroelim(elen, e, flen, f, h)  /* h cannot be e or f. */
int elen;
REAL *e;
int flen;
REAL *f;
REAL *h;
{
	REAL Q;
	INEXACT REAL Qnew;
	INEXACT REAL hh;
	INEXACT REAL bvirt;
	REAL avirt, bround, around;
	int eindex, findex, hindex;
	REAL enow, fnow;

	enow = e[0];
	fnow = f[0];
	eindex = findex = 0;
	if ((fnow > enow) == (fnow > -enow)) {
		Q = enow;
		enow = e[++eindex];
	}
	else {
		Q = fnow;
		fnow = f[++findex];
	}
	hindex = 0;
	if ((eindex < elen) && (findex < flen)) {
		if ((fnow > enow) == (fnow > -enow)) {
			Fast_Two_Sum(enow, Q, Qnew, hh);
			enow = e[++eindex];
		}
		else {
			Fast_Two_Sum(fnow, Q, Qnew, hh);
			fnow = f[++findex];
		}
		Q = Qnew;
		if (hh != 0.0) {
			h[hindex++] = hh;
		}
		while ((eindex < elen) && (findex < flen)) {
			if ((fnow > enow) == (fnow > -enow)) {
				Two_Sum(Q, enow, Qnew, hh);
				enow = e[++eindex];
			}
			else {
				Two_Sum(Q, fnow, Qnew, hh);
				fnow = f[++findex];
			}
			Q = Qnew;
			if (hh != 0.0) {
				h[hindex++] = hh;
			}
		}
	}
	while (eindex < elen) {
		Two_Sum(Q, enow, Qnew, hh);
		enow = e[++eindex];
		Q = Qnew;
		if (hh != 0.0) {
			h[hindex++] = hh;
		}
	}
	while (findex < flen) {
		Two_Sum(Q, fnow, Qnew, hh);
		fnow = f[++findex];
		Q = Qnew;
		if (hh != 0.0) {
			h[hindex++] = hh;
		}
	}
	if ((Q != 0.0) || (hindex == 0)) {
		h[hindex++] = Q;
	}
	return hindex;
}

/*****************************************************************************/
/*                                                                           */
/*  scale_expansion_zeroelim()   Multiply an expansion by a scalar,          */
/*                               eliminating zero components from the        */
/*                               output expansion.                           */
/*                                                                           */
/*  Sets h = be.  See either version of my paper for details.                */
/*                                                                           */
/*  Maintains the nonoverlapping property.  If round-to-even is used (as     */
/*  with IEEE 754), maintains the strongly nonoverlapping and nonadjacent    */
/*  properties as well.  (That is, if e has one of these properties, so      */
/*  will h.)                                                                 */
/*                                                                           */
/*****************************************************************************/

int scale_expansion_zeroelim(elen, e, b, h)   /* e and h cannot be the same. */
int elen;
REAL *e;
REAL b;
REAL *h;
{
	INEXACT REAL Q, sum;
	REAL hh;
	INEXACT REAL product1;
	REAL product0;
	int eindex, hindex;
	REAL enow;
	INEXACT REAL bvirt;
	REAL avirt, bround, around;
	INEXACT REAL c;
	INEXACT REAL abig;
	REAL ahi, alo, bhi, blo;
	REAL err1, err2, err3;

	Split(b, bhi, blo);
	Two_Product_Presplit(e[0], b, bhi, blo, Q, hh);
	hindex = 0;
	if (hh != 0) {
		h[hindex++] = hh;
	}
	for (eindex = 1; eindex < elen; eindex++) {
		enow = e[eindex];
		Two_Product_Presplit(enow, b, bhi, blo, product1, product0);
		Two_Sum(Q, product0, sum, hh);
		if (hh != 0) {
			h[hindex++] = hh;
		}
		Fast_Two_Sum(product1, sum, Q, hh);
		if (hh != 0) {
			h[hindex++] = hh;
		}
	}
	if ((Q != 0.0) || (hindex == 0)) {
		h[hindex++] = Q;
	}
	return hindex;
}

/*****************************************************************************/
/*                                                                           */
/*  estimate()   Produce a one-word estimate of an expansion's value.        */
/*                                                                           */
/*  See either version of my paper for details.                              */
/*                                                                           */
/*****************************************************************************/

REAL estimate(elen, e)
int elen;
REAL *e;
{
	REAL Q;
	int eindex;

	Q = e[0];
	for (eindex = 1; eindex < elen; eindex++) {
		Q += e[eindex];
	}
	return Q;
}

DLL_EXPORT_API REAL orient3dadapt(pa, pb, pc, pd, permanent)
REAL *pa;
REAL *pb;
REAL *pc;
REAL *pd;
REAL permanent;
{
	INEXACT REAL adx, bdx, cdx, ady, bdy, cdy, adz, bdz, cdz;
	REAL det, errbound;

	INEXACT REAL bdxcdy1, cdxbdy1, cdxady1, adxcdy1, adxbdy1, bdxady1;
	REAL bdxcdy0, cdxbdy0, cdxady0, adxcdy0, adxbdy0, bdxady0;
	REAL bc[4], ca[4], ab[4];
	INEXACT REAL bc3, ca3, ab3;
	REAL adet[8], bdet[8], cdet[8];
	int alen, blen, clen;
	REAL abdet[16];
	int ablen;
	REAL *finnow, *finother, *finswap;
	REAL fin1[192], fin2[192];
	int finlength;

	REAL adxtail, bdxtail, cdxtail;
	REAL adytail, bdytail, cdytail;
	REAL adztail, bdztail, cdztail;
	INEXACT REAL at_blarge, at_clarge;
	INEXACT REAL bt_clarge, bt_alarge;
	INEXACT REAL ct_alarge, ct_blarge;
	REAL at_b[4], at_c[4], bt_c[4], bt_a[4], ct_a[4], ct_b[4];
	int at_blen, at_clen, bt_clen, bt_alen, ct_alen, ct_blen;
	INEXACT REAL bdxt_cdy1, cdxt_bdy1, cdxt_ady1;
	INEXACT REAL adxt_cdy1, adxt_bdy1, bdxt_ady1;
	REAL bdxt_cdy0, cdxt_bdy0, cdxt_ady0;
	REAL adxt_cdy0, adxt_bdy0, bdxt_ady0;
	INEXACT REAL bdyt_cdx1, cdyt_bdx1, cdyt_adx1;
	INEXACT REAL adyt_cdx1, adyt_bdx1, bdyt_adx1;
	REAL bdyt_cdx0, cdyt_bdx0, cdyt_adx0;
	REAL adyt_cdx0, adyt_bdx0, bdyt_adx0;
	REAL bct[8], cat[8], abt[8];
	int bctlen, catlen, abtlen;
	INEXACT REAL bdxt_cdyt1, cdxt_bdyt1, cdxt_adyt1;
	INEXACT REAL adxt_cdyt1, adxt_bdyt1, bdxt_adyt1;
	REAL bdxt_cdyt0, cdxt_bdyt0, cdxt_adyt0;
	REAL adxt_cdyt0, adxt_bdyt0, bdxt_adyt0;
	REAL u[4], v[12], w[16];
	INEXACT REAL u3;
	int vlength, wlength;
	REAL negate;

	INEXACT REAL bvirt;
	REAL avirt, bround, around;
	INEXACT REAL c;
	INEXACT REAL abig;
	REAL ahi, alo, bhi, blo;
	REAL err1, err2, err3;
	INEXACT REAL _i, _j, _k;
	REAL _0;

	adx = (REAL)(pa[0] - pd[0]);
	bdx = (REAL)(pb[0] - pd[0]);
	cdx = (REAL)(pc[0] - pd[0]);
	ady = (REAL)(pa[1] - pd[1]);
	bdy = (REAL)(pb[1] - pd[1]);
	cdy = (REAL)(pc[1] - pd[1]);
	adz = (REAL)(pa[2] - pd[2]);
	bdz = (REAL)(pb[2] - pd[2]);
	cdz = (REAL)(pc[2] - pd[2]);

	Two_Product(bdx, cdy, bdxcdy1, bdxcdy0);
	Two_Product(cdx, bdy, cdxbdy1, cdxbdy0);
	Two_Two_Diff(bdxcdy1, bdxcdy0, cdxbdy1, cdxbdy0, bc3, bc[2], bc[1], bc[0]);
	bc[3] = bc3;
	alen = scale_expansion_zeroelim(4, bc, adz, adet);

	Two_Product(cdx, ady, cdxady1, cdxady0);
	Two_Product(adx, cdy, adxcdy1, adxcdy0);
	Two_Two_Diff(cdxady1, cdxady0, adxcdy1, adxcdy0, ca3, ca[2], ca[1], ca[0]);
	ca[3] = ca3;
	blen = scale_expansion_zeroelim(4, ca, bdz, bdet);

	Two_Product(adx, bdy, adxbdy1, adxbdy0);
	Two_Product(bdx, ady, bdxady1, bdxady0);
	Two_Two_Diff(adxbdy1, adxbdy0, bdxady1, bdxady0, ab3, ab[2], ab[1], ab[0]);
	ab[3] = ab3;
	clen = scale_expansion_zeroelim(4, ab, cdz, cdet);

	ablen = fast_expansion_sum_zeroelim(alen, adet, blen, bdet, abdet);
	finlength = fast_expansion_sum_zeroelim(ablen, abdet, clen, cdet, fin1);

	det = estimate(finlength, fin1);
	errbound = o3derrboundB * permanent;
	if ((det >= errbound) || (-det >= errbound)) {
		return det;
	}

	Two_Diff_Tail(pa[0], pd[0], adx, adxtail);
	Two_Diff_Tail(pb[0], pd[0], bdx, bdxtail);
	Two_Diff_Tail(pc[0], pd[0], cdx, cdxtail);
	Two_Diff_Tail(pa[1], pd[1], ady, adytail);
	Two_Diff_Tail(pb[1], pd[1], bdy, bdytail);
	Two_Diff_Tail(pc[1], pd[1], cdy, cdytail);
	Two_Diff_Tail(pa[2], pd[2], adz, adztail);
	Two_Diff_Tail(pb[2], pd[2], bdz, bdztail);
	Two_Diff_Tail(pc[2], pd[2], cdz, cdztail);

	if ((adxtail == 0.0) && (bdxtail == 0.0) && (cdxtail == 0.0)
		&& (adytail == 0.0) && (bdytail == 0.0) && (cdytail == 0.0)
		&& (adztail == 0.0) && (bdztail == 0.0) && (cdztail == 0.0)) {
		return det;
	}

	errbound = o3derrboundC * permanent + resulterrbound * Absolute(det);
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

	finnow = fin1;
	finother = fin2;

	if (adxtail == 0.0) {
		if (adytail == 0.0) {
			at_b[0] = 0.0;
			at_blen = 1;
			at_c[0] = 0.0;
			at_clen = 1;
		}
		else {
			negate = -adytail;
			Two_Product(negate, bdx, at_blarge, at_b[0]);
			at_b[1] = at_blarge;
			at_blen = 2;
			Two_Product(adytail, cdx, at_clarge, at_c[0]);
			at_c[1] = at_clarge;
			at_clen = 2;
		}
	}
	else {
		if (adytail == 0.0) {
			Two_Product(adxtail, bdy, at_blarge, at_b[0]);
			at_b[1] = at_blarge;
			at_blen = 2;
			negate = -adxtail;
			Two_Product(negate, cdy, at_clarge, at_c[0]);
			at_c[1] = at_clarge;
			at_clen = 2;
		}
		else {
			Two_Product(adxtail, bdy, adxt_bdy1, adxt_bdy0);
			Two_Product(adytail, bdx, adyt_bdx1, adyt_bdx0);
			Two_Two_Diff(adxt_bdy1, adxt_bdy0, adyt_bdx1, adyt_bdx0,
				at_blarge, at_b[2], at_b[1], at_b[0]);
			at_b[3] = at_blarge;
			at_blen = 4;
			Two_Product(adytail, cdx, adyt_cdx1, adyt_cdx0);
			Two_Product(adxtail, cdy, adxt_cdy1, adxt_cdy0);
			Two_Two_Diff(adyt_cdx1, adyt_cdx0, adxt_cdy1, adxt_cdy0,
				at_clarge, at_c[2], at_c[1], at_c[0]);
			at_c[3] = at_clarge;
			at_clen = 4;
		}
	}
	if (bdxtail == 0.0) {
		if (bdytail == 0.0) {
			bt_c[0] = 0.0;
			bt_clen = 1;
			bt_a[0] = 0.0;
			bt_alen = 1;
		}
		else {
			negate = -bdytail;
			Two_Product(negate, cdx, bt_clarge, bt_c[0]);
			bt_c[1] = bt_clarge;
			bt_clen = 2;
			Two_Product(bdytail, adx, bt_alarge, bt_a[0]);
			bt_a[1] = bt_alarge;
			bt_alen = 2;
		}
	}
	else {
		if (bdytail == 0.0) {
			Two_Product(bdxtail, cdy, bt_clarge, bt_c[0]);
			bt_c[1] = bt_clarge;
			bt_clen = 2;
			negate = -bdxtail;
			Two_Product(negate, ady, bt_alarge, bt_a[0]);
			bt_a[1] = bt_alarge;
			bt_alen = 2;
		}
		else {
			Two_Product(bdxtail, cdy, bdxt_cdy1, bdxt_cdy0);
			Two_Product(bdytail, cdx, bdyt_cdx1, bdyt_cdx0);
			Two_Two_Diff(bdxt_cdy1, bdxt_cdy0, bdyt_cdx1, bdyt_cdx0,
				bt_clarge, bt_c[2], bt_c[1], bt_c[0]);
			bt_c[3] = bt_clarge;
			bt_clen = 4;
			Two_Product(bdytail, adx, bdyt_adx1, bdyt_adx0);
			Two_Product(bdxtail, ady, bdxt_ady1, bdxt_ady0);
			Two_Two_Diff(bdyt_adx1, bdyt_adx0, bdxt_ady1, bdxt_ady0,
				bt_alarge, bt_a[2], bt_a[1], bt_a[0]);
			bt_a[3] = bt_alarge;
			bt_alen = 4;
		}
	}
	if (cdxtail == 0.0) {
		if (cdytail == 0.0) {
			ct_a[0] = 0.0;
			ct_alen = 1;
			ct_b[0] = 0.0;
			ct_blen = 1;
		}
		else {
			negate = -cdytail;
			Two_Product(negate, adx, ct_alarge, ct_a[0]);
			ct_a[1] = ct_alarge;
			ct_alen = 2;
			Two_Product(cdytail, bdx, ct_blarge, ct_b[0]);
			ct_b[1] = ct_blarge;
			ct_blen = 2;
		}
	}
	else {
		if (cdytail == 0.0) {
			Two_Product(cdxtail, ady, ct_alarge, ct_a[0]);
			ct_a[1] = ct_alarge;
			ct_alen = 2;
			negate = -cdxtail;
			Two_Product(negate, bdy, ct_blarge, ct_b[0]);
			ct_b[1] = ct_blarge;
			ct_blen = 2;
		}
		else {
			Two_Product(cdxtail, ady, cdxt_ady1, cdxt_ady0);
			Two_Product(cdytail, adx, cdyt_adx1, cdyt_adx0);
			Two_Two_Diff(cdxt_ady1, cdxt_ady0, cdyt_adx1, cdyt_adx0,
				ct_alarge, ct_a[2], ct_a[1], ct_a[0]);
			ct_a[3] = ct_alarge;
			ct_alen = 4;
			Two_Product(cdytail, bdx, cdyt_bdx1, cdyt_bdx0);
			Two_Product(cdxtail, bdy, cdxt_bdy1, cdxt_bdy0);
			Two_Two_Diff(cdyt_bdx1, cdyt_bdx0, cdxt_bdy1, cdxt_bdy0,
				ct_blarge, ct_b[2], ct_b[1], ct_b[0]);
			ct_b[3] = ct_blarge;
			ct_blen = 4;
		}
	}

	bctlen = fast_expansion_sum_zeroelim(bt_clen, bt_c, ct_blen, ct_b, bct);
	wlength = scale_expansion_zeroelim(bctlen, bct, adz, w);
	finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
		finother);
	finswap = finnow; finnow = finother; finother = finswap;

	catlen = fast_expansion_sum_zeroelim(ct_alen, ct_a, at_clen, at_c, cat);
	wlength = scale_expansion_zeroelim(catlen, cat, bdz, w);
	finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
		finother);
	finswap = finnow; finnow = finother; finother = finswap;

	abtlen = fast_expansion_sum_zeroelim(at_blen, at_b, bt_alen, bt_a, abt);
	wlength = scale_expansion_zeroelim(abtlen, abt, cdz, w);
	finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
		finother);
	finswap = finnow; finnow = finother; finother = finswap;

	if (adztail != 0.0) {
		vlength = scale_expansion_zeroelim(4, bc, adztail, v);
		finlength = fast_expansion_sum_zeroelim(finlength, finnow, vlength, v,
			finother);
		finswap = finnow; finnow = finother; finother = finswap;
	}
	if (bdztail != 0.0) {
		vlength = scale_expansion_zeroelim(4, ca, bdztail, v);
		finlength = fast_expansion_sum_zeroelim(finlength, finnow, vlength, v,
			finother);
		finswap = finnow; finnow = finother; finother = finswap;
	}
	if (cdztail != 0.0) {
		vlength = scale_expansion_zeroelim(4, ab, cdztail, v);
		finlength = fast_expansion_sum_zeroelim(finlength, finnow, vlength, v,
			finother);
		finswap = finnow; finnow = finother; finother = finswap;
	}

	if (adxtail != 0.0) {
		if (bdytail != 0.0) {
			Two_Product(adxtail, bdytail, adxt_bdyt1, adxt_bdyt0);
			Two_One_Product(adxt_bdyt1, adxt_bdyt0, cdz, u3, u[2], u[1], u[0]);
			u[3] = u3;
			finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				finother);
			finswap = finnow; finnow = finother; finother = finswap;
			if (cdztail != 0.0) {
				Two_One_Product(adxt_bdyt1, adxt_bdyt0, cdztail, u3, u[2], u[1], u[0]);
				u[3] = u3;
				finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					finother);
				finswap = finnow; finnow = finother; finother = finswap;
			}
		}
		if (cdytail != 0.0) {
			negate = -adxtail;
			Two_Product(negate, cdytail, adxt_cdyt1, adxt_cdyt0);
			Two_One_Product(adxt_cdyt1, adxt_cdyt0, bdz, u3, u[2], u[1], u[0]);
			u[3] = u3;
			finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				finother);
			finswap = finnow; finnow = finother; finother = finswap;
			if (bdztail != 0.0) {
				Two_One_Product(adxt_cdyt1, adxt_cdyt0, bdztail, u3, u[2], u[1], u[0]);
				u[3] = u3;
				finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					finother);
				finswap = finnow; finnow = finother; finother = finswap;
			}
		}
	}
	if (bdxtail != 0.0) {
		if (cdytail != 0.0) {
			Two_Product(bdxtail, cdytail, bdxt_cdyt1, bdxt_cdyt0);
			Two_One_Product(bdxt_cdyt1, bdxt_cdyt0, adz, u3, u[2], u[1], u[0]);
			u[3] = u3;
			finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				finother);
			finswap = finnow; finnow = finother; finother = finswap;
			if (adztail != 0.0) {
				Two_One_Product(bdxt_cdyt1, bdxt_cdyt0, adztail, u3, u[2], u[1], u[0]);
				u[3] = u3;
				finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					finother);
				finswap = finnow; finnow = finother; finother = finswap;
			}
		}
		if (adytail != 0.0) {
			negate = -bdxtail;
			Two_Product(negate, adytail, bdxt_adyt1, bdxt_adyt0);
			Two_One_Product(bdxt_adyt1, bdxt_adyt0, cdz, u3, u[2], u[1], u[0]);
			u[3] = u3;
			finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				finother);
			finswap = finnow; finnow = finother; finother = finswap;
			if (cdztail != 0.0) {
				Two_One_Product(bdxt_adyt1, bdxt_adyt0, cdztail, u3, u[2], u[1], u[0]);
				u[3] = u3;
				finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					finother);
				finswap = finnow; finnow = finother; finother = finswap;
			}
		}
	}
	if (cdxtail != 0.0) {
		if (adytail != 0.0) {
			Two_Product(cdxtail, adytail, cdxt_adyt1, cdxt_adyt0);
			Two_One_Product(cdxt_adyt1, cdxt_adyt0, bdz, u3, u[2], u[1], u[0]);
			u[3] = u3;
			finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				finother);
			finswap = finnow; finnow = finother; finother = finswap;
			if (bdztail != 0.0) {
				Two_One_Product(cdxt_adyt1, cdxt_adyt0, bdztail, u3, u[2], u[1], u[0]);
				u[3] = u3;
				finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					finother);
				finswap = finnow; finnow = finother; finother = finswap;
			}
		}
		if (bdytail != 0.0) {
			negate = -cdxtail;
			Two_Product(negate, bdytail, cdxt_bdyt1, cdxt_bdyt0);
			Two_One_Product(cdxt_bdyt1, cdxt_bdyt0, adz, u3, u[2], u[1], u[0]);
			u[3] = u3;
			finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
				finother);
			finswap = finnow; finnow = finother; finother = finswap;
			if (adztail != 0.0) {
				Two_One_Product(cdxt_bdyt1, cdxt_bdyt0, adztail, u3, u[2], u[1], u[0]);
				u[3] = u3;
				finlength = fast_expansion_sum_zeroelim(finlength, finnow, 4, u,
					finother);
				finswap = finnow; finnow = finother; finother = finswap;
			}
		}
	}

	if (adztail != 0.0) {
		wlength = scale_expansion_zeroelim(bctlen, bct, adztail, w);
		finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
			finother);
		finswap = finnow; finnow = finother; finother = finswap;
	}
	if (bdztail != 0.0) {
		wlength = scale_expansion_zeroelim(catlen, cat, bdztail, w);
		finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
			finother);
		finswap = finnow; finnow = finother; finother = finswap;
	}
	if (cdztail != 0.0) {
		wlength = scale_expansion_zeroelim(abtlen, abt, cdztail, w);
		finlength = fast_expansion_sum_zeroelim(finlength, finnow, wlength, w,
			finother);
		finswap = finnow; finnow = finother; finother = finswap;
	}

	return finnow[finlength - 1];
}

DLL_EXPORT_API REAL orient3d(pa, pb, pc, pd)
REAL *pa;
REAL *pb;
REAL *pc;
REAL *pd;
{
	REAL adx, bdx, cdx, ady, bdy, cdy, adz, bdz, cdz;
	REAL bdxcdy, cdxbdy, cdxady, adxcdy, adxbdy, bdxady;
	REAL det;
	REAL permanent, errbound;

	adx = pa[0] - pd[0];
	bdx = pb[0] - pd[0];
	cdx = pc[0] - pd[0];
	ady = pa[1] - pd[1];
	bdy = pb[1] - pd[1];
	cdy = pc[1] - pd[1];
	adz = pa[2] - pd[2];
	bdz = pb[2] - pd[2];
	cdz = pc[2] - pd[2];

	bdxcdy = bdx * cdy;
	cdxbdy = cdx * bdy;

	cdxady = cdx * ady;
	adxcdy = adx * cdy;

	adxbdy = adx * bdy;
	bdxady = bdx * ady;

	det = adz * (bdxcdy - cdxbdy)
		+ bdz * (cdxady - adxcdy)
		+ cdz * (adxbdy - bdxady);

	permanent = (Absolute(bdxcdy) + Absolute(cdxbdy)) * Absolute(adz)
		+ (Absolute(cdxady) + Absolute(adxcdy)) * Absolute(bdz)
		+ (Absolute(adxbdy) + Absolute(bdxady)) * Absolute(cdz);
	errbound = o3derrboundA * permanent;
	if ((det > errbound) || (-det > errbound)) {
		return det;
	}

	return orient3dadapt(pa, pb, pc, pd, permanent);
}

/*****************************************************************************/
/*                                                                           */
/*  insphereexact()   Exact 3D insphere test.  Robust.                       */
/*  insphere()   Adaptive exact 3D insphere test.  Robust.                   */
/*                                                                           */
/*               Return a positive value if the point pe lies inside the     */
/*               sphere passing through pa, pb, pc, and pd; a negative value */
/*               if it lies outside; and zero if the five points are         */
/*               cospherical.  The points pa, pb, pc, and pd must be ordered */
/*               so that they have a positive orientation (as defined by     */
/*               orient3d()), or the sign of the result will be reversed.    */
/*                                                                           */
/*  Only the first and last routine should be used; the middle two are for   */
/*  timings.                                                                 */
/*                                                                           */
/*  The last three use exact arithmetic to ensure a correct answer.  The     */
/*  result returned is the determinant of a matrix.  In insphere() only,     */
/*  this determinant is computed adaptively, in the sense that exact         */
/*  arithmetic is used only to the degree it is needed to ensure that the    */
/*  returned value has the correct sign.  Hence, insphere() is usually quite */
/*  fast, but will run more slowly when the input points are cospherical or  */
/*  nearly so.                                                               */
/*                                                                           */
/*****************************************************************************/

DLL_EXPORT_API REAL insphereexact(pa, pb, pc, pd, pe)
REAL *pa;
REAL *pb;
REAL *pc;
REAL *pd;
REAL *pe;
{
	INEXACT REAL axby1, bxcy1, cxdy1, dxey1, exay1;
	INEXACT REAL bxay1, cxby1, dxcy1, exdy1, axey1;
	INEXACT REAL axcy1, bxdy1, cxey1, dxay1, exby1;
	INEXACT REAL cxay1, dxby1, excy1, axdy1, bxey1;
	REAL axby0, bxcy0, cxdy0, dxey0, exay0;
	REAL bxay0, cxby0, dxcy0, exdy0, axey0;
	REAL axcy0, bxdy0, cxey0, dxay0, exby0;
	REAL cxay0, dxby0, excy0, axdy0, bxey0;
	REAL ab[4], bc[4], cd[4], de[4], ea[4];
	REAL ac[4], bd[4], ce[4], da[4], eb[4];
	REAL temp8a[8], temp8b[8], temp16[16];
	int temp8alen, temp8blen, temp16len;
	REAL abc[24], bcd[24], cde[24], dea[24], eab[24];
	REAL abd[24], bce[24], cda[24], deb[24], eac[24];
	int abclen, bcdlen, cdelen, dealen, eablen;
	int abdlen, bcelen, cdalen, deblen, eaclen;
	REAL temp48a[48], temp48b[48];
	int temp48alen, temp48blen;
	REAL abcd[96], bcde[96], cdea[96], deab[96], eabc[96];
	int abcdlen, bcdelen, cdealen, deablen, eabclen;
	REAL temp192[192];
	REAL det384x[384], det384y[384], det384z[384];
	int xlen, ylen, zlen;
	REAL detxy[768];
	int xylen;
	REAL adet[1152], bdet[1152], cdet[1152], ddet[1152], edet[1152];
	int alen, blen, clen, dlen, elen;
	REAL abdet[2304], cddet[2304], cdedet[3456];
	int ablen, cdlen;
	REAL deter[5760];
	int deterlen;
	int i;

	INEXACT REAL bvirt;
	REAL avirt, bround, around;
	INEXACT REAL c;
	INEXACT REAL abig;
	REAL ahi, alo, bhi, blo;
	REAL err1, err2, err3;
	INEXACT REAL _i, _j;
	REAL _0;

	Two_Product(pa[0], pb[1], axby1, axby0);
	Two_Product(pb[0], pa[1], bxay1, bxay0);
	Two_Two_Diff(axby1, axby0, bxay1, bxay0, ab[3], ab[2], ab[1], ab[0]);

	Two_Product(pb[0], pc[1], bxcy1, bxcy0);
	Two_Product(pc[0], pb[1], cxby1, cxby0);
	Two_Two_Diff(bxcy1, bxcy0, cxby1, cxby0, bc[3], bc[2], bc[1], bc[0]);

	Two_Product(pc[0], pd[1], cxdy1, cxdy0);
	Two_Product(pd[0], pc[1], dxcy1, dxcy0);
	Two_Two_Diff(cxdy1, cxdy0, dxcy1, dxcy0, cd[3], cd[2], cd[1], cd[0]);

	Two_Product(pd[0], pe[1], dxey1, dxey0);
	Two_Product(pe[0], pd[1], exdy1, exdy0);
	Two_Two_Diff(dxey1, dxey0, exdy1, exdy0, de[3], de[2], de[1], de[0]);

	Two_Product(pe[0], pa[1], exay1, exay0);
	Two_Product(pa[0], pe[1], axey1, axey0);
	Two_Two_Diff(exay1, exay0, axey1, axey0, ea[3], ea[2], ea[1], ea[0]);

	Two_Product(pa[0], pc[1], axcy1, axcy0);
	Two_Product(pc[0], pa[1], cxay1, cxay0);
	Two_Two_Diff(axcy1, axcy0, cxay1, cxay0, ac[3], ac[2], ac[1], ac[0]);

	Two_Product(pb[0], pd[1], bxdy1, bxdy0);
	Two_Product(pd[0], pb[1], dxby1, dxby0);
	Two_Two_Diff(bxdy1, bxdy0, dxby1, dxby0, bd[3], bd[2], bd[1], bd[0]);

	Two_Product(pc[0], pe[1], cxey1, cxey0);
	Two_Product(pe[0], pc[1], excy1, excy0);
	Two_Two_Diff(cxey1, cxey0, excy1, excy0, ce[3], ce[2], ce[1], ce[0]);

	Two_Product(pd[0], pa[1], dxay1, dxay0);
	Two_Product(pa[0], pd[1], axdy1, axdy0);
	Two_Two_Diff(dxay1, dxay0, axdy1, axdy0, da[3], da[2], da[1], da[0]);

	Two_Product(pe[0], pb[1], exby1, exby0);
	Two_Product(pb[0], pe[1], bxey1, bxey0);
	Two_Two_Diff(exby1, exby0, bxey1, bxey0, eb[3], eb[2], eb[1], eb[0]);

	temp8alen = scale_expansion_zeroelim(4, bc, pa[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, ac, -pb[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, ab, pc[2], temp8a);
	abclen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		abc);

	temp8alen = scale_expansion_zeroelim(4, cd, pb[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, bd, -pc[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, bc, pd[2], temp8a);
	bcdlen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		bcd);

	temp8alen = scale_expansion_zeroelim(4, de, pc[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, ce, -pd[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, cd, pe[2], temp8a);
	cdelen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		cde);

	temp8alen = scale_expansion_zeroelim(4, ea, pd[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, da, -pe[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, de, pa[2], temp8a);
	dealen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		dea);

	temp8alen = scale_expansion_zeroelim(4, ab, pe[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, eb, -pa[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, ea, pb[2], temp8a);
	eablen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		eab);

	temp8alen = scale_expansion_zeroelim(4, bd, pa[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, da, pb[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, ab, pd[2], temp8a);
	abdlen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		abd);

	temp8alen = scale_expansion_zeroelim(4, ce, pb[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, eb, pc[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, bc, pe[2], temp8a);
	bcelen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		bce);

	temp8alen = scale_expansion_zeroelim(4, da, pc[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, ac, pd[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, cd, pa[2], temp8a);
	cdalen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		cda);

	temp8alen = scale_expansion_zeroelim(4, eb, pd[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, bd, pe[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, de, pb[2], temp8a);
	deblen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		deb);

	temp8alen = scale_expansion_zeroelim(4, ac, pe[2], temp8a);
	temp8blen = scale_expansion_zeroelim(4, ce, pa[2], temp8b);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp8blen, temp8b,
		temp16);
	temp8alen = scale_expansion_zeroelim(4, ea, pc[2], temp8a);
	eaclen = fast_expansion_sum_zeroelim(temp8alen, temp8a, temp16len, temp16,
		eac);

	temp48alen = fast_expansion_sum_zeroelim(cdelen, cde, bcelen, bce, temp48a);
	temp48blen = fast_expansion_sum_zeroelim(deblen, deb, bcdlen, bcd, temp48b);
	for (i = 0; i < temp48blen; i++) {
		temp48b[i] = -temp48b[i];
	}
	bcdelen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
		temp48blen, temp48b, bcde);
	xlen = scale_expansion_zeroelim(bcdelen, bcde, pa[0], temp192);
	xlen = scale_expansion_zeroelim(xlen, temp192, pa[0], det384x);
	ylen = scale_expansion_zeroelim(bcdelen, bcde, pa[1], temp192);
	ylen = scale_expansion_zeroelim(ylen, temp192, pa[1], det384y);
	zlen = scale_expansion_zeroelim(bcdelen, bcde, pa[2], temp192);
	zlen = scale_expansion_zeroelim(zlen, temp192, pa[2], det384z);
	xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	alen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, adet);

	temp48alen = fast_expansion_sum_zeroelim(dealen, dea, cdalen, cda, temp48a);
	temp48blen = fast_expansion_sum_zeroelim(eaclen, eac, cdelen, cde, temp48b);
	for (i = 0; i < temp48blen; i++) {
		temp48b[i] = -temp48b[i];
	}
	cdealen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
		temp48blen, temp48b, cdea);
	xlen = scale_expansion_zeroelim(cdealen, cdea, pb[0], temp192);
	xlen = scale_expansion_zeroelim(xlen, temp192, pb[0], det384x);
	ylen = scale_expansion_zeroelim(cdealen, cdea, pb[1], temp192);
	ylen = scale_expansion_zeroelim(ylen, temp192, pb[1], det384y);
	zlen = scale_expansion_zeroelim(cdealen, cdea, pb[2], temp192);
	zlen = scale_expansion_zeroelim(zlen, temp192, pb[2], det384z);
	xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	blen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, bdet);

	temp48alen = fast_expansion_sum_zeroelim(eablen, eab, deblen, deb, temp48a);
	temp48blen = fast_expansion_sum_zeroelim(abdlen, abd, dealen, dea, temp48b);
	for (i = 0; i < temp48blen; i++) {
		temp48b[i] = -temp48b[i];
	}
	deablen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
		temp48blen, temp48b, deab);
	xlen = scale_expansion_zeroelim(deablen, deab, pc[0], temp192);
	xlen = scale_expansion_zeroelim(xlen, temp192, pc[0], det384x);
	ylen = scale_expansion_zeroelim(deablen, deab, pc[1], temp192);
	ylen = scale_expansion_zeroelim(ylen, temp192, pc[1], det384y);
	zlen = scale_expansion_zeroelim(deablen, deab, pc[2], temp192);
	zlen = scale_expansion_zeroelim(zlen, temp192, pc[2], det384z);
	xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	clen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, cdet);

	temp48alen = fast_expansion_sum_zeroelim(abclen, abc, eaclen, eac, temp48a);
	temp48blen = fast_expansion_sum_zeroelim(bcelen, bce, eablen, eab, temp48b);
	for (i = 0; i < temp48blen; i++) {
		temp48b[i] = -temp48b[i];
	}
	eabclen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
		temp48blen, temp48b, eabc);
	xlen = scale_expansion_zeroelim(eabclen, eabc, pd[0], temp192);
	xlen = scale_expansion_zeroelim(xlen, temp192, pd[0], det384x);
	ylen = scale_expansion_zeroelim(eabclen, eabc, pd[1], temp192);
	ylen = scale_expansion_zeroelim(ylen, temp192, pd[1], det384y);
	zlen = scale_expansion_zeroelim(eabclen, eabc, pd[2], temp192);
	zlen = scale_expansion_zeroelim(zlen, temp192, pd[2], det384z);
	xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	dlen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, ddet);

	temp48alen = fast_expansion_sum_zeroelim(bcdlen, bcd, abdlen, abd, temp48a);
	temp48blen = fast_expansion_sum_zeroelim(cdalen, cda, abclen, abc, temp48b);
	for (i = 0; i < temp48blen; i++) {
		temp48b[i] = -temp48b[i];
	}
	abcdlen = fast_expansion_sum_zeroelim(temp48alen, temp48a,
		temp48blen, temp48b, abcd);
	xlen = scale_expansion_zeroelim(abcdlen, abcd, pe[0], temp192);
	xlen = scale_expansion_zeroelim(xlen, temp192, pe[0], det384x);
	ylen = scale_expansion_zeroelim(abcdlen, abcd, pe[1], temp192);
	ylen = scale_expansion_zeroelim(ylen, temp192, pe[1], det384y);
	zlen = scale_expansion_zeroelim(abcdlen, abcd, pe[2], temp192);
	zlen = scale_expansion_zeroelim(zlen, temp192, pe[2], det384z);
	xylen = fast_expansion_sum_zeroelim(xlen, det384x, ylen, det384y, detxy);
	elen = fast_expansion_sum_zeroelim(xylen, detxy, zlen, det384z, edet);

	ablen = fast_expansion_sum_zeroelim(alen, adet, blen, bdet, abdet);
	cdlen = fast_expansion_sum_zeroelim(clen, cdet, dlen, ddet, cddet);
	cdelen = fast_expansion_sum_zeroelim(cdlen, cddet, elen, edet, cdedet);
	deterlen = fast_expansion_sum_zeroelim(ablen, abdet, cdelen, cdedet, deter);

	return deter[deterlen - 1];
}

DLL_EXPORT_API REAL insphereadapt(pa, pb, pc, pd, pe, permanent)
REAL *pa;
REAL *pb;
REAL *pc;
REAL *pd;
REAL *pe;
REAL permanent;
{
	INEXACT REAL aex, bex, cex, dex, aey, bey, cey, dey, aez, bez, cez, dez;
	REAL det, errbound;

	INEXACT REAL aexbey1, bexaey1, bexcey1, cexbey1;
	INEXACT REAL cexdey1, dexcey1, dexaey1, aexdey1;
	INEXACT REAL aexcey1, cexaey1, bexdey1, dexbey1;
	REAL aexbey0, bexaey0, bexcey0, cexbey0;
	REAL cexdey0, dexcey0, dexaey0, aexdey0;
	REAL aexcey0, cexaey0, bexdey0, dexbey0;
	REAL ab[4], bc[4], cd[4], da[4], ac[4], bd[4];
	INEXACT REAL ab3, bc3, cd3, da3, ac3, bd3;
	REAL abeps, bceps, cdeps, daeps, aceps, bdeps;
	REAL temp8a[8], temp8b[8], temp8c[8], temp16[16], temp24[24], temp48[48];
	int temp8alen, temp8blen, temp8clen, temp16len, temp24len, temp48len;
	REAL xdet[96], ydet[96], zdet[96], xydet[192];
	int xlen, ylen, zlen, xylen;
	REAL adet[288], bdet[288], cdet[288], ddet[288];
	int alen, blen, clen, dlen;
	REAL abdet[576], cddet[576];
	int ablen, cdlen;
	REAL fin1[1152];
	int finlength;

	REAL aextail, bextail, cextail, dextail;
	REAL aeytail, beytail, ceytail, deytail;
	REAL aeztail, beztail, ceztail, deztail;

	INEXACT REAL bvirt;
	REAL avirt, bround, around;
	INEXACT REAL c;
	INEXACT REAL abig;
	REAL ahi, alo, bhi, blo;
	REAL err1, err2, err3;
	INEXACT REAL _i, _j;
	REAL _0;

	aex = (REAL)(pa[0] - pe[0]);
	bex = (REAL)(pb[0] - pe[0]);
	cex = (REAL)(pc[0] - pe[0]);
	dex = (REAL)(pd[0] - pe[0]);
	aey = (REAL)(pa[1] - pe[1]);
	bey = (REAL)(pb[1] - pe[1]);
	cey = (REAL)(pc[1] - pe[1]);
	dey = (REAL)(pd[1] - pe[1]);
	aez = (REAL)(pa[2] - pe[2]);
	bez = (REAL)(pb[2] - pe[2]);
	cez = (REAL)(pc[2] - pe[2]);
	dez = (REAL)(pd[2] - pe[2]);

	Two_Product(aex, bey, aexbey1, aexbey0);
	Two_Product(bex, aey, bexaey1, bexaey0);
	Two_Two_Diff(aexbey1, aexbey0, bexaey1, bexaey0, ab3, ab[2], ab[1], ab[0]);
	ab[3] = ab3;

	Two_Product(bex, cey, bexcey1, bexcey0);
	Two_Product(cex, bey, cexbey1, cexbey0);
	Two_Two_Diff(bexcey1, bexcey0, cexbey1, cexbey0, bc3, bc[2], bc[1], bc[0]);
	bc[3] = bc3;

	Two_Product(cex, dey, cexdey1, cexdey0);
	Two_Product(dex, cey, dexcey1, dexcey0);
	Two_Two_Diff(cexdey1, cexdey0, dexcey1, dexcey0, cd3, cd[2], cd[1], cd[0]);
	cd[3] = cd3;

	Two_Product(dex, aey, dexaey1, dexaey0);
	Two_Product(aex, dey, aexdey1, aexdey0);
	Two_Two_Diff(dexaey1, dexaey0, aexdey1, aexdey0, da3, da[2], da[1], da[0]);
	da[3] = da3;

	Two_Product(aex, cey, aexcey1, aexcey0);
	Two_Product(cex, aey, cexaey1, cexaey0);
	Two_Two_Diff(aexcey1, aexcey0, cexaey1, cexaey0, ac3, ac[2], ac[1], ac[0]);
	ac[3] = ac3;

	Two_Product(bex, dey, bexdey1, bexdey0);
	Two_Product(dex, bey, dexbey1, dexbey0);
	Two_Two_Diff(bexdey1, bexdey0, dexbey1, dexbey0, bd3, bd[2], bd[1], bd[0]);
	bd[3] = bd3;

	temp8alen = scale_expansion_zeroelim(4, cd, bez, temp8a);
	temp8blen = scale_expansion_zeroelim(4, bd, -cez, temp8b);
	temp8clen = scale_expansion_zeroelim(4, bc, dez, temp8c);
	temp16len = fast_expansion_sum_zeroelim(temp8alen, temp8a,
		temp8blen, temp8b, temp16);
	temp24len = fast_expansion_sum_zeroelim(temp8clen, temp8c,
		temp16len, temp16, temp24);
	temp48len = scale_expansion_zeroelim(temp24len, temp24, aex, temp48);
	xlen = scale_expansion_zeroelim(temp48len, temp48, -aex, xdet);
	temp48len = scale_expansion_zeroelim(temp24len, temp24, aey, temp48);
	ylen = scale_expansion_zeroelim(temp48len, temp48, -aey, ydet);
	temp48len = scale_expansion_zeroelim(temp24len, temp24, aez, temp48);
	zlen = scale_expansion_zeroelim(temp48len, temp48, -aez, zdet);
	xylen = fast_expansion_sum_zeroelim(xlen, xdet, ylen, ydet, xydet);
	alen = fast_expansion_sum_zeroelim(xylen, xydet, zlen, zdet, adet);

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
	blen = fast_expansion_sum_zeroelim(xylen, xydet, zlen, zdet, bdet);

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
	clen = fast_expansion_sum_zeroelim(xylen, xydet, zlen, zdet, cdet);

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
	dlen = fast_expansion_sum_zeroelim(xylen, xydet, zlen, zdet, ddet);

	ablen = fast_expansion_sum_zeroelim(alen, adet, blen, bdet, abdet);
	cdlen = fast_expansion_sum_zeroelim(clen, cdet, dlen, ddet, cddet);
	finlength = fast_expansion_sum_zeroelim(ablen, abdet, cdlen, cddet, fin1);

	det = estimate(finlength, fin1);
	errbound = isperrboundB * permanent;
	if ((det >= errbound) || (-det >= errbound)) {
		return det;
	}

	Two_Diff_Tail(pa[0], pe[0], aex, aextail);
	Two_Diff_Tail(pa[1], pe[1], aey, aeytail);
	Two_Diff_Tail(pa[2], pe[2], aez, aeztail);
	Two_Diff_Tail(pb[0], pe[0], bex, bextail);
	Two_Diff_Tail(pb[1], pe[1], bey, beytail);
	Two_Diff_Tail(pb[2], pe[2], bez, beztail);
	Two_Diff_Tail(pc[0], pe[0], cex, cextail);
	Two_Diff_Tail(pc[1], pe[1], cey, ceytail);
	Two_Diff_Tail(pc[2], pe[2], cez, ceztail);
	Two_Diff_Tail(pd[0], pe[0], dex, dextail);
	Two_Diff_Tail(pd[1], pe[1], dey, deytail);
	Two_Diff_Tail(pd[2], pe[2], dez, deztail);
	if ((aextail == 0.0) && (aeytail == 0.0) && (aeztail == 0.0)
		&& (bextail == 0.0) && (beytail == 0.0) && (beztail == 0.0)
		&& (cextail == 0.0) && (ceytail == 0.0) && (ceztail == 0.0)
		&& (dextail == 0.0) && (deytail == 0.0) && (deztail == 0.0)) {
		return det;
	}

	errbound = isperrboundC * permanent + resulterrbound * Absolute(det);
	abeps = (aex * beytail + bey * aextail)
		- (aey * bextail + bex * aeytail);
	bceps = (bex * ceytail + cey * bextail)
		- (bey * cextail + cex * beytail);
	cdeps = (cex * deytail + dey * cextail)
		- (cey * dextail + dex * ceytail);
	daeps = (dex * aeytail + aey * dextail)
		- (dey * aextail + aex * deytail);
	aceps = (aex * ceytail + cey * aextail)
		- (aey * cextail + cex * aeytail);
	bdeps = (bex * deytail + dey * bextail)
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
		+ 2.0 * (((bex * bextail + bey * beytail + bez * beztail)
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

	return insphereexact(pa, pb, pc, pd, pe);
}

DLL_EXPORT_API REAL insphere(pa, pb, pc, pd, pe)
REAL *pa;
REAL *pb;
REAL *pc;
REAL *pd;
REAL *pe;
{
	REAL aex, bex, cex, dex;
	REAL aey, bey, cey, dey;
	REAL aez, bez, cez, dez;
	REAL aexbey, bexaey, bexcey, cexbey, cexdey, dexcey, dexaey, aexdey;
	REAL aexcey, cexaey, bexdey, dexbey;
	REAL alift, blift, clift, dlift;
	REAL ab, bc, cd, da, ac, bd;
	REAL abc, bcd, cda, dab;
	REAL aezplus, bezplus, cezplus, dezplus;
	REAL aexbeyplus, bexaeyplus, bexceyplus, cexbeyplus;
	REAL cexdeyplus, dexceyplus, dexaeyplus, aexdeyplus;
	REAL aexceyplus, cexaeyplus, bexdeyplus, dexbeyplus;
	REAL det;
	REAL permanent, errbound;

	aex = pa[0] - pe[0];
	bex = pb[0] - pe[0];
	cex = pc[0] - pe[0];
	dex = pd[0] - pe[0];
	aey = pa[1] - pe[1];
	bey = pb[1] - pe[1];
	cey = pc[1] - pe[1];
	dey = pd[1] - pe[1];
	aez = pa[2] - pe[2];
	bez = pb[2] - pe[2];
	cez = pc[2] - pe[2];
	dez = pd[2] - pe[2];

	aexbey = aex * bey;
	bexaey = bex * aey;
	ab = aexbey - bexaey;
	bexcey = bex * cey;
	cexbey = cex * bey;
	bc = bexcey - cexbey;
	cexdey = cex * dey;
	dexcey = dex * cey;
	cd = cexdey - dexcey;
	dexaey = dex * aey;
	aexdey = aex * dey;
	da = dexaey - aexdey;

	aexcey = aex * cey;
	cexaey = cex * aey;
	ac = aexcey - cexaey;
	bexdey = bex * dey;
	dexbey = dex * bey;
	bd = bexdey - dexbey;

	abc = aez * bc - bez * ac + cez * ab;
	bcd = bez * cd - cez * bd + dez * bc;
	cda = cez * da + dez * ac + aez * cd;
	dab = dez * ab + aez * bd + bez * da;

	alift = aex * aex + aey * aey + aez * aez;
	blift = bex * bex + bey * bey + bez * bez;
	clift = cex * cex + cey * cey + cez * cez;
	dlift = dex * dex + dey * dey + dez * dez;

	det = (dlift * abc - clift * dab) + (blift * cda - alift * bcd);

	aezplus = Absolute(aez);
	bezplus = Absolute(bez);
	cezplus = Absolute(cez);
	dezplus = Absolute(dez);
	aexbeyplus = Absolute(aexbey);
	bexaeyplus = Absolute(bexaey);
	bexceyplus = Absolute(bexcey);
	cexbeyplus = Absolute(cexbey);
	cexdeyplus = Absolute(cexdey);
	dexceyplus = Absolute(dexcey);
	dexaeyplus = Absolute(dexaey);
	aexdeyplus = Absolute(aexdey);
	aexceyplus = Absolute(aexcey);
	cexaeyplus = Absolute(cexaey);
	bexdeyplus = Absolute(bexdey);
	dexbeyplus = Absolute(dexbey);
	permanent = ((cexdeyplus + dexceyplus) * bezplus
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
	errbound = isperrboundA * permanent;
	if ((det > errbound) || (-det > errbound)) {
		return det;
	}

	return insphereadapt(pa, pb, pc, pd, pe, permanent);
}
