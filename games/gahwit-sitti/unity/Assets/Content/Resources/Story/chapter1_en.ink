// Chapter 1: Homecoming, in English. Translated by Claude Code from chapter1.ink; it follows that file line
// for line (same knots, lines, speakers and choice ids; the parity test in Story/Tests checks it). Change the dialect
// file first, then this one. [[word]] marks a tap-to-explain word; its English meaning is in the Glossary table.

EXTERNAL chose(knot)

=== intro ===
Here it is… [[Teta]]'s café. The sign hangs crooked, and the chairs are piled up in the corner. #salma
“Salma, my heart, a café isn't walls, it's people. Open it, and the people will come back.” #teta
-> END

=== abu_fadi_arrives ===
Welcome, Khalil's girl! You're the image of your Teta. Will you make me a cup? It's been ages since I had coffee made properly. #abu_fadi
-> END

=== sign ===
Look at the sign! It's 1974 all over again. Your Teta sat right here every morning, watering the basil. #abu_fadi
Uncle Abu Fadi, tell me about her. I was away for so long. #salma
She used to say: the first cup is for the guest, the second for talk, the third for love. Come every day, and I'll tell you. #abu_fadi
-> END

=== floor ===
Look at these tiles! Teta used to say they're older than the whole neighbourhood. #salma
Every tile has a story. I broke my first cup right here as a boy, and your Teta never said one harsh word. #abu_fadi
-> END

=== chairs ===
This is my table. Thirty years I've sat here, facing the window. #abu_fadi
That's settled, Uncle. From today, this table is reserved for you. #salma
-> END

=== dallahs ===
They shine like new… Teta polished them every Friday. #salma
And the little [[dallah]]? That one was hers alone. No one poured from it but her. #abu_fadi
* [“Tell me about her, Uncle.” #ask]
    Some things you're better off learning from her. Look through her notebooks. #abu_fadi
* [“You'll tell me tomorrow, right?” #tomorrow]
    Tomorrow… if the cup comes out right. #abu_fadi
- -> END

=== lights ===
The café's lit up! Even the neighbours are leaning out of their windows. #salma
A café with its lights on never stays empty. You'll see tomorrow. #abu_fadi
-> END

=== counter ===
Here you are, Uncle Abu Fadi. The first cup from the little dallah. #salma
That's how she made it… with cardamom, and never in a hurry. God rest her soul. #abu_fadi
{
- chose("dallahs") == "ask":
    You asked me about her, and I didn't answer. That's how she was: she said more with a cup than with words. #abu_fadi
- else:
    The cup came out right… and I promised you a story. But not today. Today I want to drink. #abu_fadi
}
At the back of the recipe book, a page is torn out: “[[Knafeh]] Im Khalil”… Who took it? #sys
-> END

=== m1_lead ===
Oh… this is her coffee. The same cardamom, the same faint bite at the end. #abu_fadi
The first time I sat at this table, I was twenty-five. And your Teta was your age. #abu_fadi
-> END

=== m1_intro ===
Welcome, my café's first customer! What will you have, sir? #teta_young
Coffee… and a smile too, if there's one going. #fadi_young
Just the coffee, Mr. Fadi. The smile is reserved for my husband. #teta_young
-> END

=== m1_perfect ===
Exactly like that. Coffee needs patience and a steady hand. #teta_young
-> END

=== m1_ok ===
Never mind. Coffee forgives, like good people do. #teta_young
-> END

=== m1_outro ===
What will you call the café? It needs a name. #fadi_young
It has no name yet. When my children come, I'll name it after them. #teta_young
Two girls came: Rana and Nawal. Years later, Teta wrote a letter and hid it under the counter. #sys
Fifty years, and no one read it. #sys
-> END

=== m1_return ===
Your Teta, God rest her, used to say: this counter has a secret, but it isn't time yet. #abu_fadi
A secret? What secret, Uncle? #salma
Ask the drawer, my girl. I'll drink my coffee and keep quiet. #abu_fadi
-> END

=== letter_1 ===
To my daughters, Rana and Nawal, #letter
If you're reading this letter, it means I couldn't say it to your faces. #letter
The café belongs to you both. Not because you've made peace… because I want you to. #letter
The mistake was mine from the start, not either of yours. #letter
And if you want to know what happened in 2006… #letter_faded
— Your mother #letter_signature
-> END

=== voice_1 ===
Salma, my heart… if you've found the letter, make two cups. One for you, and one for Lana. #teta
-> END

=== lana_notify ===
Landed in Amman. Mom says she doesn't want to see my aunt… and I want to know why. #lana
-> END
