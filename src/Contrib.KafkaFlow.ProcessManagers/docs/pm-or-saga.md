## "Process Manager" or "Saga"?

Strictly speaking, using term "Saga" for "Process Manager" isn't correct because
these are two different _patterns_.

**Process Manager** is a generic pattern to orchestrate _processes_ in a way of
"when _this_ happened, what should happen _next_".

**Saga** is a pattern that helps with _reverting multi-transactional action_.
It answers the question of "when something happened, and then the next thing failed,
how do I make sure that all the steps, this and all before it, are reverted?".

As a pattern, "Saga" means "this is a process that can completely be reverted in some (failure)
condition, and here is how".

Sagas are typically implemented as process managers (or live within them), that's why some people may
interchange these terms.

This way we can see Sagas as Process Managers, but not all Process Managers are Sagas!
In fact, the majority of them are not!

So, saying **Saga** instead of **Process Manager** is like
saying **Apple** instead of **Fruit**.
Or, maybe, like saying **Disposable** instead of saying **Object**, because not all objects,
and even if some are, the _focus_ of the conveyed meaning could be different.
