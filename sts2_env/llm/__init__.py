"""Local-LLM run agent: serialize state to text, parse a choice back.

Combat is handled by the deterministic planner; the model only makes
out-of-combat decisions, so it is a drop-in replacement for the RL run agent
and the scripted KnowledgeRunPolicy and is measured on the same protocol.
"""
