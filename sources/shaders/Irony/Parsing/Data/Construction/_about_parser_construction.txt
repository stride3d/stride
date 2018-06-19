  About parser construction algorithm in general
  We follow DeRemer-Penello's algorithm, as it is described in Grune, Jacobs "Parsing Techniques" 2nd ed, section 9.7, p. 309.
  There are a few differences:
   1. We compute lookbacks and transitions "on-demand" - only those that are actually needed for computing lookaheads in 
      reduce items in inadequate states. We start with reduce items in inadequate states - those are the only items that need lookaheads.
      We then find all lookbacks (transitions) for these items. Then for each transition we find which ones need to "include" other parent
      transitions - and compute this. And so on, until all transitions are created and linked through Include relationships
   2. We propagate Include relation between transitions immediately, when we add an include relation of one transition to another. See 
      Transition.Include method. Thus we avoid an extra step of "Transitive closure" of Include relation. See note about efficiency below.     
   3. We don't use Reads and DirectRead relation between transitions. "Reads" relation 
      between transitions is replaced by Reads relation between states. So state A READS state B if you can move from state A to state B
      using shifts over nullable terminals. ParserStateData.ReadStateSet contains all states that current state Reads. ReadStateSet 
      is computed on-demand, and all reads are immediately propagated through transitive chain - see source code of the method. 
      For DirectReads set for a transition in DeRemer-Penello - we use a state.ShiftTerminals set of the target state of the transition 
       - obviously this is the same set.

 Note about immediate Include propagation
  I think that the method with immediate Includes propagation is as efficient as it can be, and using Transitive Closure optimization
  through Srongly-Connected Components (SCC) algorithm would not be much faster. With immediate propagation we attempt to add 
  a transition to Includes set of another transition only once and stop propagation of the transition further down the chain if it is
  already there. Essentially, we don't waste time propagating sets of transitions through chains of Includes if the transitions are 
  alredy there, propagated through different route. This is what SCC method is trying to mitigate - repeated propagation of transitions - 
  but this is not happening in our implementation. Maybe I'm mistaken, this is a guess, not a formal proof - let me know if you see 
  any flaws in my reasoning.
 
 About computing ExpectedTerminals set for parser states.
  ExpectedTerms is a property of ParserState and contains all Terminals that parser expects in this state. This set is used by Scanner 
  which Terminal to use for recongizing next token when it has a choice of more than one. (This is called Scanner-Parser link facility).
  The question now is how to compute this set. There are are several kinds of Parser states:
    1. Containing shift items only. The ExpectedSet is a union of all "current" terms of all shift items. State.BuilderData.ShiftTerms 
       already contains this set - easy case.
    2. Containing shift AND reduce items. This is inadequate set. The expected set is a union of all current terms of shift items 
       (like in previous case) plus all lookaheads of reduce items. Reduce items have lookaheads computed, because it is inadequate state.
    3. Containing 2 or more reduce items - this is again an inadequate state, each reduce items has lookaheads computed, so expected set
       is a union of lookaheads of reduce items.
    4. Containing a single reduce item. This is a troubled case. The state is not inadequate, so we don't compute lookaheads for a single
       reduce items - there is no need for them, only a single action is possible. 
  The solution for the last case with a single reduce item is the following: we do not compute ExpectedSet for such states, but make sure 
  that scanner-parser link is never activated in this case. We do it in Parser code by NOT reading the next token from Scanner when 
  current state has a single reduce action (DefaultReduceAction property is not null). We do not read next token because it is not needed
  for finding an action - there is one single possible action anyway. As a result the Scanner would never start scanning a new token 
  when parser in this single-reduce state - and therefore scanner would not invoke the parser-scanner link. 
  See CoreParser.ExecuteAction method for details.
 
